using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ContosoSupportCopilot.Infrastructure.Data;

public static class CopilotDatabaseInitializer
{
    // Copilot state has its own database. EnsureCreated adds tables only to a database that has
    // none, so a second DbContext on the support database would never get its tables.
    public static async Task InitializeCopilotDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CopilotDbContext>();
        if (await dbContext.Database.EnsureCreatedAsync(cancellationToken))
        {
            return;
        }

        // A database from an earlier chapter: add the tables this chapter introduced, keep the data.
        var created = await dbContext.CreateMissingTablesAsync(cancellationToken);
        if (created.Count > 0)
        {
            scope.ServiceProvider.GetRequiredService<ILogger<CopilotDbContext>>()
                .LogInformation("Added tables to the copilot database: {Tables}", string.Join(", ", created));
        }
    }
}
