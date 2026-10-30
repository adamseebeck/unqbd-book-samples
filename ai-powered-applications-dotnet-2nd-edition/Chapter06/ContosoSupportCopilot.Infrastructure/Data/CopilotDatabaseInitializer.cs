using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoSupportCopilot.Infrastructure.Data;

public static class CopilotDatabaseInitializer
{
    // Copilot state has its own database. EnsureCreated adds tables only to a database that has
    // none, so a second DbContext on the support database would never get its tables.
    public static async Task InitializeCopilotDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CopilotDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }
}
