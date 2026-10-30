using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using ContosoSupportCopilot.Domain.Helpers;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace ContosoSupportCopilot.Infrastructure.AI;

public class DraftingService : IDraftingService
{
    private readonly IChatClient _chatClient;
    private readonly ILogger<DraftingService> _logger;

    public DraftingService(
        IChatClient chatClient,
        ILogger<DraftingService> logger)
    {
        _chatClient = chatClient;
        _logger = logger;
    }

    public async IAsyncEnumerable<string> DraftReplyStreamingAsync(
        SupportTicket ticket,
        DraftingOptions options,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var updates = new List<ChatResponseUpdate>();
        long? firstTokenMs = null;
        var stopwatch = Stopwatch.StartNew();

        await foreach (var update in _chatClient.GetStreamingResponseAsync(
            BuildMessages(ticket), ToChatOptions(options), cancellationToken))
        {
            updates.Add(update);
            if (update.Text.Length > 0)
            {
                firstTokenMs ??= stopwatch.ElapsedMilliseconds;
                yield return update.Text;
            }
        }

        stopwatch.Stop();

        // The final updates carry usage; ToChatResponse combines the stream into one response.
        var usage = updates.ToChatResponse().Usage;
        _logger.LogInformation(
            "Streaming draft completed for ticket {TicketId}. First text after {FirstTokenMs}ms, total {LatencyMs}ms. Tokens: {Input}/{Output}",
            ticket.Id,
            firstTokenMs,
            stopwatch.ElapsedMilliseconds,
            usage?.InputTokenCount,
            usage?.OutputTokenCount);
    }

    public async Task<string> DraftReplyAsync(
        SupportTicket ticket,
        DraftingOptions options,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var response = await _chatClient.GetResponseAsync(BuildMessages(ticket), ToChatOptions(options), cancellationToken);
        stopwatch.Stop();

        _logger.LogInformation(
            "Draft generated for ticket {TicketId}. Tokens: {Input}/{Output}, Latency: {LatencyMs}ms",
            ticket.Id,
            response.Usage?.InputTokenCount,
            response.Usage?.OutputTokenCount,
            stopwatch.ElapsedMilliseconds);

        return response.Text;
    }

    // Temperature stays null unless the caller sets it for a model that accepts it.
    private static ChatOptions ToChatOptions(DraftingOptions options) => new()
    {
        MaxOutputTokens = options.MaxOutputTokens,
        Temperature = options.Temperature
    };

    private static List<ChatMessage> BuildMessages(SupportTicket ticket) =>
    [
        new(ChatRole.System, GetSystemPrompt()),
        new(ChatRole.User, BuildPrompt(ticket))
    ];

    private static string GetSystemPrompt()
    {
        return """
            You are a senior support engineer for Contoso. Draft professional, empathetic replies
            to customer tickets. Acknowledge the customer's issue clearly, provide the solution
            or next steps, and maintain a tone appropriate to their account tier.
            Do not ask for information already provided in the ticket history.
            """;
    }

    // Ticket values are escaped, so text such as "</subject>" cannot close a tag or open a new one.
    // The tags lower the risk of prompt injection; they do not prevent it.
    private static string BuildPrompt(SupportTicket ticket)
    {
        var builder = new StringBuilder();

        builder.AppendLine(AccountContextHelper.FormatAccountContext(ticket.Customer));
        builder.AppendLine($"<subject>{SecurityElement.Escape(ticket.Subject)}</subject>");
        builder.AppendLine($"<description>{SecurityElement.Escape(ticket.Description)}</description>");
        builder.AppendLine($"<product_area>{SecurityElement.Escape(ticket.ProductArea)}</product_area>");
        builder.AppendLine("<conversation_history>");

        foreach (var message in ticket.Messages.OrderBy(m => m.SentAt))
        {
            var sender = message.IsFromCustomer ? "Customer" : "Support";
            builder.AppendLine($"<{sender}>{SecurityElement.Escape(message.Content)}</{sender}>");
        }

        builder.AppendLine("</conversation_history>");
        builder.AppendLine("<instruction>Draft a reply:</instruction>");

        return builder.ToString();
    }
}
