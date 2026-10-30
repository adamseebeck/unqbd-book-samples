using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ContosoSupportCopilot.Infrastructure.Data;

public static class SupportDatabaseInitializer
{
    // Creates the database on first run and adds the sample tickets and articles.
    // Use EF Core migrations instead of EnsureCreated once the schema must evolve.
    public static async Task InitializeSupportDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SupportDbContext>();

        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
        if (!await dbContext.SupportTickets.AnyAsync(cancellationToken))
        {
            dbContext.SupportTickets.AddRange(SampleData.CreateTickets());
            dbContext.KnowledgeArticles.AddRange(SampleData.CreateArticles());
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
