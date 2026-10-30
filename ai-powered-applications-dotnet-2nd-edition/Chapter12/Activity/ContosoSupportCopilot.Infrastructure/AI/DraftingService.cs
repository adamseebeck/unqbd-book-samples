using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security;
using System.Text;
using System.Xml.Linq;
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

        // The chat client retries the stream only until its first update, because after that the engineer
        // already sees text. FallbackDraftingService handles a failure later in the stream.
        await foreach (var update in _chatClient.GetStreamingResponseAsync(
            BuildMessages(ticket, options), ToChatOptions(options), cancellationToken))
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

    public async Task<DraftReply> DraftReplyAsync(
        SupportTicket ticket,
        DraftingOptions options,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        // The chat client runs the call through the Chapter 4 pipeline: it limits each attempt and the whole
        // call, retries transient failures, and opens its circuit when the service keeps failing.
        // FallbackDraftingService handles what it throws.
        var response = await _chatClient.GetResponseAsync(BuildMessages(ticket, options), ToChatOptions(options), cancellationToken);
        stopwatch.Stop();

        _logger.LogInformation(
            "Draft generated for ticket {TicketId}. Tokens: {Input}/{Output}, Latency: {LatencyMs}ms",
            ticket.Id,
            response.Usage?.InputTokenCount,
            response.Usage?.OutputTokenCount,
            stopwatch.ElapsedMilliseconds);

        // The model and token counts travel with the text, so callers can record them.
        return new DraftReply
        {
            Text = response.Text,
            ModelId = response.ModelId ?? string.Empty,
            InputTokens = (int)(response.Usage?.InputTokenCount ?? 0),
            OutputTokens = (int)(response.Usage?.OutputTokenCount ?? 0)
        };
    }

    // Temperature stays null unless the caller sets it for a model that accepts it.
    private static ChatOptions ToChatOptions(DraftingOptions options) => new()
    {
        MaxOutputTokens = options.MaxOutputTokens,
        Temperature = options.Temperature
    };

    private static List<ChatMessage> BuildMessages(SupportTicket ticket, DraftingOptions options) =>
    [
        new(ChatRole.System, GetSystemPrompt()),
        new(ChatRole.User, BuildPrompt(ticket, options.Knowledge))
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
    private static string BuildPrompt(SupportTicket ticket, DraftingKnowledge? knowledge)
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

        if (knowledge is null)
        {
            builder.AppendLine("<instruction>Draft a reply:</instruction>");
            return builder.ToString();
        }

        // From Chapter 10: the knowledge agent's cited answer and excerpts. As in Chapter 8's prompt builder,
        // XmlSafe removes what XML 1.0 cannot hold, such as half an emoji where an excerpt was cut, and LINQ to
        // XML escapes the rest, so they stay data inside their elements.
        builder.AppendLine(new XElement("knowledge_answer", PromptTextHelper.XmlSafe(knowledge.Answer)).ToString());
        builder.AppendLine(new XElement("sources", knowledge.Sources.Select(source => new XElement("source",
            new XAttribute("title", PromptTextHelper.XmlSafe(source.ArticleTitle)),
            PromptTextHelper.XmlSafe(source.Excerpt)))).ToString());
        builder.AppendLine("<instruction>Draft a reply. Take product facts and steps only from the knowledge answer " +
            "and its sources, and leave out the citation markers.</instruction>");

        return builder.ToString();
    }
}
