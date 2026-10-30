using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Repositories;
using ContosoSupportCopilot.Infrastructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoSupportCopilot.Tests;

// Runs against a real SQLite database in memory, so EF Core's change tracking is part of the test.
public sealed class SqlConversationRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public SqlConversationRepositoryTests()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task SaveAsync_AddsNewTurns_ToAnExistingConversation()
    {
        var conversation = new CopilotConversation
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            CurrentTurn = 1,
            Turns = [new ConversationTurn { TurnNumber = 1, UserInput = "First question", CopilotResponse = "First answer" }]
        };

        await using (var context = CreateContext())
        {
            await new SqlConversationRepository(context).SaveAsync(conversation);
        }

        // A later request loads the conversation, adds a turn, and saves it.
        await using (var context = CreateContext())
        {
            var repository = new SqlConversationRepository(context);
            var loaded = await repository.GetByTicketAsync(conversation.TicketId);
            Assert.NotNull(loaded);

            var turn = new ConversationTurn { TurnNumber = 2, UserInput = "Second question", CopilotResponse = "Second answer" };
            await repository.SaveAsync(loaded with { CurrentTurn = 2, Turns = [.. loaded.Turns, turn] });
        }

        await using (var context = CreateContext())
        {
            var saved = await new SqlConversationRepository(context).GetAsync(conversation.Id);
            Assert.NotNull(saved);
            Assert.Equal(2, saved.CurrentTurn);
            Assert.Equal(["First question", "Second question"], saved.Turns.OrderBy(t => t.TurnNumber).Select(t => t.UserInput));
        }
    }

    [Fact]
    public async Task SaveAsync_RejectsAStaleSave_WhenAnotherRequestSavedATurnFirst()
    {
        var conversation = new CopilotConversation
        {
            Id = Guid.NewGuid(),
            TicketId = Guid.NewGuid(),
            CurrentTurn = 1,
            Turns = [new ConversationTurn { TurnNumber = 1, UserInput = "First question" }]
        };
        await using (var context = CreateContext())
        {
            await new SqlConversationRepository(context).SaveAsync(conversation);
        }

        // Two requests load turn 1; the first saves turn 2, and the second save starts from the old count.
        await using var first = CreateContext();
        await using var second = CreateContext();
        var firstRepository = new SqlConversationRepository(first);
        var secondRepository = new SqlConversationRepository(second);
        var loadedByFirst = (await firstRepository.GetByTicketAsync(conversation.TicketId))!;
        var loadedBySecond = (await secondRepository.GetByTicketAsync(conversation.TicketId))!;

        await firstRepository.SaveAsync(NextTurn(loadedByFirst, "From the first request"));

        await Assert.ThrowsAsync<ConversationConflictException>(() =>
            secondRepository.SaveAsync(NextTurn(loadedBySecond, "From the second request")));

        // After the conflict, the second request reads the conversation as it is now.
        var reloaded = await secondRepository.GetByTicketAsync(conversation.TicketId);
        Assert.Equal(2, reloaded!.CurrentTurn);
        Assert.Equal("From the first request", reloaded.Turns.Single(t => t.TurnNumber == 2).UserInput);
    }

    [Fact]
    public async Task SaveAsync_AllowsOneActiveConversationPerTicket()
    {
        var ticketId = Guid.NewGuid();
        await using (var context = CreateContext())
        {
            await new SqlConversationRepository(context).SaveAsync(NewConversation(ticketId));
        }

        await using (var context = CreateContext())
        {
            await Assert.ThrowsAsync<ConversationConflictException>(() =>
                new SqlConversationRepository(context).SaveAsync(NewConversation(ticketId)));
        }
    }

    private static CopilotConversation NextTurn(CopilotConversation conversation, string userInput) => conversation with
    {
        CurrentTurn = conversation.CurrentTurn + 1,
        Turns = [.. conversation.Turns, new ConversationTurn { TurnNumber = conversation.CurrentTurn + 1, UserInput = userInput }]
    };

    private static CopilotConversation NewConversation(Guid ticketId) => new()
    {
        Id = Guid.NewGuid(),
        TicketId = ticketId,
        CurrentTurn = 1,
        Turns = [new ConversationTurn { TurnNumber = 1, UserInput = "First question" }]
    };

    private CopilotDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<CopilotDbContext>().UseSqlite(_connection).Options);

    public void Dispose() => _connection.Dispose();
}
