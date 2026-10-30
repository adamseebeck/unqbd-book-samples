using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace ContosoSupportCopilot.Tests.Fixtures;

// The real RAG pipeline from Chapter 8 on the offline chat client, with deterministic stand-ins
// for embeddings, vector search and the knowledge base: no model, no database, no network.
public sealed class AIIntegrationTestFixture : IDisposable
{
    public static readonly KnowledgeArticle Article = new()
    {
        Id = Guid.Parse("5b1a9c2e-0001-4f00-9000-000000000001"),
        Title = "Unlock an account after a password reset",
        Content = "An administrator clears the lockout from the user page. The user then signs in with the new password.",
        ProductArea = "Authentication",
        IsPublished = true
    };

    private readonly IHost _host;

    public AIIntegrationTestFixture()
    {
        VectorSearch.AddResult(new VectorSearchResult
        {
            Chunk = new ArticleChunk { Id = Guid.NewGuid(), ChunkIndex = 0, Content = Article.Content },
            Score = 0.82,
            ArticleId = Article.Id,
            ArticleTitle = Article.Title,
            ProductArea = Article.ProductArea
        });

        var knowledgeBase = new Mock<ISupportContext>();
        knowledgeBase.Setup(k => k.GetArticlesAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<Guid> ids, CancellationToken _) => ids.Contains(Article.Id) ? [Article] : []);

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["AI:Provider"] = "Offline",
            ["Rag:ProductAreasByEngineer:engineer-ana:0"] = "Authentication",
            ["Logging:LogLevel:Default"] = "Warning"
        });
        builder.Services.AddAIProvider(builder.Configuration);
        builder.Services.AddSemanticKernelServices(builder.Configuration);
        builder.Services.AddRagServices(builder.Configuration);

        // The stand-ins replace the embedding client, the vector store and the database.
        builder.Services.AddSingleton<IEmbeddingClient>(Embeddings);
        builder.Services.AddSingleton<IVectorSearchService>(VectorSearch);
        builder.Services.AddScoped(_ => knowledgeBase.Object);
        _host = builder.Build();
    }

    public MockEmbeddingClient Embeddings { get; } = new();

    public FakeVectorSearchService VectorSearch { get; } = new();

    public IServiceScope CreateScope() => _host.Services.CreateScope();

    public void Dispose() => _host.Dispose();
}
