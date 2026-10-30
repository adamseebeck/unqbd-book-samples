using System.Diagnostics;
using System.Security;
using System.Text.Json;
using System.Text.Json.Serialization;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Configuration;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.AI;

public class IntentExtractor : IIntentExtractor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    private const double MinimumConfidence = 0.6;

    // From lowest to highest; an unknown value ranks below all of them.
    private static readonly string[] Urgencies = ["Low", "Normal", "High", "Critical"];

    private readonly IChatClient _chatClient;
    private readonly TokenPricingOptions _pricing;
    private readonly ILogger<IntentExtractor> _logger;
    private readonly IFallbackIntentExtractor? _fallback;

    public IntentExtractor(
        IChatClient chatClient,
        IOptions<TokenPricingOptions> pricing,
        ILogger<IntentExtractor> logger,
        IFallbackIntentExtractor? fallback = null)
    {
        _chatClient = chatClient;
        _pricing = pricing.Value;
        _logger = logger;
        _fallback = fallback;
    }

    public async Task<SupportIntent> ExtractAsync(
        string ticketDescription,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(30));

        var systemPrompt = """
            You are a support triage assistant. Analyze the ticket description and extract:
            - Intent: The customer's goal (e.g., "PasswordReset", "FeatureRequest", "BugReport")
            - Urgency: "Low", "Normal", "High", or "Critical"
            - ProductArea: The relevant product module (e.g., "Authentication", "Payments", "API")
            - ConfidenceScore: A value between 0.0 and 1.0

            Respond with valid JSON only. Use this exact structure:
            {"Intent": "...", "Urgency": "...", "ProductArea": "...", "ConfidenceScore": 0.0}
            Do not include markdown formatting or explanations.
            """;

        // Escaping keeps the ticket text from closing the tag around it. The tags make the
        // boundary clear; they lower the risk of prompt injection but cannot prevent it.
        List<ChatMessage> messages =
        [
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, $"""
                <ticket_description>
                {SecurityElement.Escape(ticketDescription)}
                </ticket_description>
                """)
        ];

        var options = new ChatOptions
        {
            ResponseFormat = ChatResponseFormat.Json,
            MaxOutputTokens = 2000
        };

        var stopwatch = Stopwatch.StartNew();
        ChatResponse response;

        try
        {
            response = await _chatClient.GetResponseAsync(messages, options, cts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError("Intent extraction timed out after 30 seconds");
            throw new TimeoutException("The AI service did not respond within the allocated time.");
        }

        stopwatch.Stop();

        var usage = response.Usage;
        _logger.LogInformation(
            "Intent extraction completed in {LatencyMs}ms. Tokens: {Input} input ({Cached} cached), {Output} output ({Reasoning} reasoning). Cost: ${Cost:F6}",
            stopwatch.ElapsedMilliseconds,
            usage?.InputTokenCount,
            usage?.CachedInputTokenCount,
            usage?.OutputTokenCount,
            usage?.ReasoningTokenCount,
            TokenCostCalculator.Calculate(usage, _pricing));

        if (response.FinishReason == ChatFinishReason.Length)
        {
            throw new InvalidOperationException(
                "The response reached MaxOutputTokens before the JSON was complete. Raise the limit or lower the reasoning effort.");
        }

        try
        {
            var intent = JsonSerializer.Deserialize<SupportIntent>(response.Text.Trim(), JsonOptions);

            if (intent == null || string.IsNullOrEmpty(intent.Intent))
            {
                throw new InvalidOperationException("Deserialized intent is null or missing required fields.");
            }

            if (intent.ConfidenceScore < MinimumConfidence && _fallback is not null)
            {
                _logger.LogWarning(
                    "Intent confidence {Confidence} is below {Threshold}; using the fallback extractor",
                    intent.ConfidenceScore,
                    MinimumConfidence);
                var fallback = await _fallback.ExtractFallbackAsync(ticketDescription, cancellationToken);

                // An uncertain answer can still report an outage, so the fallback never lowers the urgency.
                return Array.IndexOf(Urgencies, intent.Urgency) > Array.IndexOf(Urgencies, fallback.Urgency)
                    ? fallback with { Urgency = intent.Urgency }
                    : fallback;
            }

            return intent;
        }
        catch (JsonException ex)
        {
            // The answer can echo customer data from the ticket, so only its length goes to the error log.
            _logger.LogError(ex, "Failed to parse the intent JSON ({Length} characters)", response.Text.Length);
            _logger.LogTrace("Unparsed intent answer: {Content}", response.Text);
            throw new InvalidOperationException("The model returned invalid JSON structure.", ex);
        }
    }
}
