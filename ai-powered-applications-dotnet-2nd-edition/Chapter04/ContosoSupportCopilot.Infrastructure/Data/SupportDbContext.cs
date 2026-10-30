using ContosoSupportCopilot.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoSupportCopilot.Infrastructure.Data;

public class SupportDbContext : DbContext
{
    public SupportDbContext(DbContextOptions<SupportDbContext> options) : base(options)
    {
    }

    public DbSet<SupportTicket> SupportTickets { get; set; } = null!;
    public DbSet<KnowledgeArticle> KnowledgeArticles { get; set; } = null!;
    public DbSet<TicketMessage> TicketMessages { get; set; } = null!;
}
