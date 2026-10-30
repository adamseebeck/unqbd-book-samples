using System.Diagnostics;
using ContosoSupportCopilot.Application.Services;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Prompts;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Domain.Validation;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.AI.Offline;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Data;
using ContosoSupportCopilot.Infrastructure.Resilience;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using Polly.Registry;

namespace ContosoSupportCopilot.Api;

/// <summary>
/// Runs this chapter's main path once and exits:
/// dotnet run --project ContosoSupportCopilot.Api -- --check
/// </summary>
public static class ChapterCheck
{
    private static readonly string[] SampleDescriptions =
    [
        "I reset my password this morning and now every sign in attempt says my account is locked. Our team is blocked.",
        "Our card was charged twice for the October invoice. Please refund the duplicate payment.",
        "Our nightly integration job gets HTTP 429 responses from the orders endpoint. How should we handle the rate limit?",
        "Something seems off with my account since yesterday."
    ];

    public static async Task<int> RunAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var chat = provider.GetRequiredService<IChatClient>().GetService<ChatClientMetadata>();

        Console.WriteLine($"Contoso Support Copilot: Chapter 5 activity check (chat provider: {chat?.ProviderName}, model: {chat?.DefaultModelId})");
        if (chat?.ProviderName == "Offline")
        {
            Console.WriteLine("Offline provider: answers are local sample text and token counts are estimates.");
        }

        // The JSON schema that GetResponseAsync<SupportIntent> sends with every request.
        Console.WriteLine($"Schema: {ChatResponseFormat.ForJsonSchema<SupportIntent>(StructuredOutputJson.Options).Schema}");

        var extractor = provider.GetRequiredService<IIntentExtractor>();
        Console.WriteLine($"Intent extractor: {extractor.GetType().Name}");
        foreach (var description in SampleDescriptions)
        {
            var intent = await extractor.ExtractAsync(description);
            Console.WriteLine($"Intent: {intent.Intent} | Urgency: {intent.Urgency} | Area: {intent.ProductArea} | Confidence: {intent.ConfidenceScore:F2}");
        }

        var draft = await provider.GetRequiredService<EscalationDraftService>().GenerateDraftAsync(SampleData.PasswordTicketId);
        Console.WriteLine($"Escalation draft with history: {draft.RecommendedPriority} priority. {draft.Summary} Steps: {draft.ReproductionSteps}");

        if (!await CheckValidationRetriesAsync(provider))
        {
            Console.WriteLine("Check failed: invalid answers were not retried and rejected as expected.");
            return 1;
        }

        Console.WriteLine("Check passed.");
        return 0;
    }

    // Sends scripted bad answers through the real pipeline to show retries and rejection.
    private static async Task<bool> CheckValidationRetriesAsync(IServiceProvider provider)
    {
        var pipelines = provider.GetRequiredService<ResiliencePipelineProvider<string>>();
        var logger = provider.GetRequiredService<ILogger<ValidatedPromptExecutor>>();
        var options = provider.GetRequiredService<IOptions<AIProviderOptions>>();
        var pricing = provider.GetRequiredService<IOptions<TokenPricingOptions>>();
        var validator = provider.GetRequiredService<IOutputValidator<SupportIntent>>();
        var variables = new Dictionary<string, object> { ["ticketDescription"] = "Every sign in attempt says my account is locked." };

        // 1. JSON in a Markdown fence, then a string where a number belongs, then a valid answer.
        var recovering = new ScriptedResponder(
            "```json\n{\"intent\":\"PasswordReset\",\"urgency\":\"High\",\"productArea\":\"Authentication\",\"confidenceScore\":0.9}\n```",
            "{\"intent\":\"PasswordReset\",\"urgency\":\"High\",\"productArea\":\"Authentication\",\"confidenceScore\":\"high\"}",
            "{\"intent\":\"PasswordReset\",\"urgency\":\"High\",\"productArea\":\"Authentication\",\"confidenceScore\":0.9}");
        var executor = new ValidatedPromptExecutor(new OfflineChatClient([recovering]), pipelines, options, pricing, logger);
        var intent = await executor.ExecuteAsync(SupportPrompts.IntentExtraction, variables, validator);
        Console.WriteLine($"Validation retries: valid answer on attempt {recovering.Calls}: {intent.Intent} | {intent.Urgency} | {intent.ConfidenceScore:F2}");

        // 2. Well-formed JSON that breaks the rules every time: three attempts, then an exception.
        var breaking = new ScriptedResponder(Enumerable.Repeat(
            "{\"intent\":\"PasswordReset\",\"urgency\":\"Medium\",\"productArea\":\"Authentication\",\"confidenceScore\":1.4}", 3).ToArray());
        var rejected = false;
        try
        {
            await new ValidatedPromptExecutor(new OfflineChatClient([breaking]), pipelines, options, pricing, logger)
                .ExecuteAsync(SupportPrompts.IntentExtraction, variables, validator);
        }
        catch (SchemaValidationException ex)
        {
            Console.WriteLine($"Persistent invalid output: rejected after {breaking.Calls} attempts: {string.Join("; ", ex.ValidationErrors)}");
            rejected = recovering.Calls == 3 && breaking.Calls == 3;
        }

        // 3. Invalid answers that take a second and a half each: one deadline (here two seconds) ends the
        // whole structured call, validation retries included, during the second attempt.
        var shortDeadline = new ResiliencePipelineRegistry<string>();
        shortDeadline.TryAddBuilder(StructuredOutputPipeline.Name, (builder, _) =>
            builder.AddStructuredOutputResilience(new KernelResilienceOptions { TotalTimeoutSeconds = 2 }));
        var slow = new SlowChatClient(new OfflineChatClient([new ScriptedResponder(breaking.Answers)]), TimeSpan.FromSeconds(1.5));
        var stopwatch = Stopwatch.StartNew();
        var timedOut = false;
        try
        {
            await new ValidatedPromptExecutor(slow, shortDeadline, options, pricing, logger)
                .ExecuteAsync(SupportPrompts.IntentExtraction, variables, validator);
        }
        catch (TimeoutException)
        {
            Console.WriteLine($"Slow invalid answers: TimeoutException after {slow.Attempts} attempts in {stopwatch.Elapsed.TotalSeconds:F1} s");
            timedOut = slow.Attempts == 2;
        }

        return rejected && timedOut;
    }

    // Returns prepared answers in order, so the check can show what happens with bad output.
    private sealed class ScriptedResponder(params string[] answers) : IOfflineResponder
    {
        public int Calls { get; private set; }

        public string[] Answers => answers;

        public string? TryRespond(OfflineChatRequest request) => Calls < answers.Length ? answers[Calls++] : null;
    }

    // Waits before every answer, like a slow deployment.
    private sealed class SlowChatClient(IChatClient innerClient, TimeSpan delay) : DelegatingChatClient(innerClient)
    {
        public int Attempts { get; private set; }

        public override async Task<ChatResponse> GetResponseAsync(
            IEnumerable<ChatMessage> messages,
            ChatOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            Attempts++;
            await Task.Delay(delay, cancellationToken);
            return await base.GetResponseAsync(messages, options, cancellationToken);
        }
    }
}
