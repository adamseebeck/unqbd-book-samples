using System.ComponentModel;
using ContosoSupportCopilot.Domain.Services;
using Microsoft.SemanticKernel;

namespace ContosoSupportCopilot.Infrastructure.Plugins;

public class SupportTicketPlugin
{
    private readonly ISupportContext _supportContext;

    public SupportTicketPlugin(ISupportContext supportContext)
    {
        _supportContext = supportContext;
    }

    [KernelFunction("calculate_priority_score")]
    [Description("Calculates a numeric priority score based on ticket priority, customer tier, and ticket age")]
    public async Task<double> CalculatePriorityScoreAsync(
        [Description("The unique identifier of the support ticket")] Guid ticketId,
        CancellationToken cancellationToken = default)
    {
        var ticket = await _supportContext.GetTicketAsync(ticketId, cancellationToken);
        if (ticket == null)
        {
            return 0.0;
        }

        double score = ticket.Priority switch
        {
            "Critical" => 100.0,
            "High" => 75.0,
            "Normal" => 50.0,
            "Low" => 25.0,
            _ => 50.0
        };

        if (ticket.Customer?.Tier == "Enterprise")
        {
            score *= 1.5;
        }
        else if (ticket.Customer?.Tier == "Professional")
        {
            score *= 1.2;
        }

        var age = DateTime.UtcNow - ticket.CreatedAt;
        if (age.TotalHours > 24)
        {
            score += 10.0;
        }

        return Math.Min(score, 150.0);
    }

    [KernelFunction("extract_keywords")]
    [Description("Extracts relevant technical keywords from ticket content")]
    public Task<string[]> ExtractKeywordsAsync(
        [Description("The text content to analyze")] string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return Task.FromResult(Array.Empty<string>());
        }

        var keywords = new List<string>();
        var lowerContent = content.ToLowerInvariant();

        if (lowerContent.Contains("authentication") || lowerContent.Contains("login"))
        {
            keywords.Add("authentication");
        }

        if (lowerContent.Contains("performance") || lowerContent.Contains("slow"))
        {
            keywords.Add("performance");
        }

        if (lowerContent.Contains("error") || lowerContent.Contains("exception"))
        {
            keywords.Add("error");
        }

        if (lowerContent.Contains("database") || lowerContent.Contains("sql"))
        {
            keywords.Add("database");
        }

        return Task.FromResult(keywords.ToArray());
    }
}
