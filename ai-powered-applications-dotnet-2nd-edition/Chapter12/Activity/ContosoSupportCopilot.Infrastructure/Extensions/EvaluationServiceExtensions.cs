using ContosoSupportCopilot.Domain.Evaluation;
using ContosoSupportCopilot.Infrastructure.AI;
using ContosoSupportCopilot.Infrastructure.Configuration;
using ContosoSupportCopilot.Infrastructure.Evaluation;
using ContosoSupportCopilot.Infrastructure.Resilience;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.AI.Evaluation;
using Microsoft.Extensions.AI.Evaluation.Quality;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ContosoSupportCopilot.Infrastructure.Extensions;

public static class EvaluationServiceExtensions
{
    // Call after AddRagServices: the evaluation runs the RAG pipeline from Chapter 8.
    public static IServiceCollection AddEvaluationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<EvaluationOptions>()
            .Bind(configuration.GetSection(EvaluationOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Deterministic evaluators need no model, so they run offline, in tests and in CI.
        services.AddSingleton<IEvaluator, RetrievalPrecisionEvaluator>();
        services.AddSingleton<IEvaluator, CitationUsefulnessEvaluator>();
        services.AddSingleton<IEvaluator, SourceOverlapEvaluator>();

        // LLM judges from Microsoft.Extensions.AI.Evaluation.Quality, with a chat client of their own.
        services.AddKeyedSingleton<IEvaluator, GroundednessEvaluator>(RagEvaluationService.JudgeKey);
        services.AddKeyedSingleton<IEvaluator, RelevanceEvaluator>(RagEvaluationService.JudgeKey);
        services.AddKeyedSingleton<IEvaluator, RetrievalEvaluator>(RagEvaluationService.JudgeKey);
        services.AddKeyedSingleton<IEvaluator, CompletenessEvaluator>(RagEvaluationService.JudgeKey);

        // The judges' client is not the registered IChatClient, so it gets the Chapter 4 resilience middleware
        // here, once. JudgeChatClient, outside it, removes the sampling options that most reasoning deployments
        // reject, and a judgment cut off at the output limit fails without a retry.
        services.AddKeyedSingleton<IChatClient>(RagEvaluationService.JudgeKey, (sp, _) =>
        {
            var options = sp.GetRequiredService<IOptions<EvaluationOptions>>().Value;
            var judgeModel = sp.GetRequiredService<AIClientFactory>().CreateChatClient(options.JudgeDeployment);
            return new ChatClientBuilder(judgeModel)
                .Use(inner => new JudgeChatClient(inner, options.JudgeMaxOutputTokens))
                .UseKernelResilience()
                .Build(sp);
        });

        services.AddSingleton<IRetrievalOptimizer, RetrievalOptimizer>();
        services.AddSingleton<IPromptRevisionStrategy, PromptRevisionStrategy>();

        // Scoped like the RAG orchestrator it runs.
        services.AddScoped<RagEvaluationService>();

        return services;
    }
}
