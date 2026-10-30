using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public sealed class RagPipelineIntegrationTests : IClassFixture<AIIntegrationTestFixture>
{
    private readonly AIIntegrationTestFixture _fixture;

    public RagPipelineIntegrationTests(AIIntegrationTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task GroundedAnswer_CitesTheSeededArticle()
    {
        using var scope = _fixture.CreateScope();
        var ticket = new SupportTicket { Id = Guid.NewGuid(), Subject = "Locked out after a password reset", ProductArea = "Authentication" };

        var result = await scope.ServiceProvider.GetRequiredService<IRagOrchestrator>().GenerateGroundedResponseAsync(
            new RagRequest { TicketId = ticket.Id, Query = ticket.Subject, ProductArea = ticket.ProductArea, RequesterId = "engineer-ana" },
            ticket);

        Assert.True(result.IsGrounded);
        Assert.Equal([AIIntegrationTestFixture.Article.Id], result.CitedArticleIds);
        Assert.Contains("[Article: Unlock an account after a password reset", result.Response);
    }

    [Fact]
    public async Task MockEmbeddingClient_GivesTheSameVectorInEveryRun()
    {
        var first = await _fixture.Embeddings.GenerateAsync("Locked out after a password reset");
        var second = await new MockEmbeddingClient().GenerateAsync("  locked out after a password reset ");

        // A fixed value, not just equality within one process: the seed comes from SHA-256.
        Assert.Equal(first.Vector, second.Vector);
        Assert.Equal(MockEmbeddingClient.DefaultDimensions, first.Vector.Length);
        Assert.Equal(-0.5646f, first.Vector[0], 0.0001f);
    }

    [Fact]
    public async Task FakeVectorSearch_AppliesTheProductAreaAndTheFloor()
    {
        var vector = (await _fixture.Embeddings.GenerateAsync("anything")).Vector;

        var payments = await _fixture.VectorSearch.SearchAsync(vector, new VectorSearchRequest { TopK = 5, ProductAreaFilter = "Payments" });
        var strict = await _fixture.VectorSearch.SearchAsync(vector, new VectorSearchRequest { TopK = 5, MinScore = 0.9 });
        var authentication = await _fixture.VectorSearch.SearchAsync(vector, new VectorSearchRequest { TopK = 5, ProductAreaFilter = "Authentication" });

        Assert.Empty(payments);
        Assert.Empty(strict);
        Assert.Single(authentication);
    }
}
