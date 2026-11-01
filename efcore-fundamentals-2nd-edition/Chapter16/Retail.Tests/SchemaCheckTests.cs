using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

public sealed class SchemaCheckTests : IAsyncLifetime
{
    private readonly TestDatabase database = new();

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => database.DeleteAsync();

    [Fact]
    public async Task EmptyDatabaseFailsTheCheck()
    {
        await using var db = database.CreateContext();
        await db.GetService<IRelationalDatabaseCreator>()
            .CreateAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => SchemaCheck.EnsureCurrentAsync(db));
    }

    [Fact]
    public async Task MigratedDatabasePassesTheCheck()
    {
        await database.CreateAsync();
        await using var db = database.CreateContext();
        await SchemaCheck.EnsureCurrentAsync(db);
    }
}
