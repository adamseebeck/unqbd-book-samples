using System.Text;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Infrastructure.AI.Offline;

/// <summary>
/// Lets a chapter's code answer specific offline requests, for example to return a
/// malformed response that exercises validation. Return null to let the next responder,
/// or the built-in sample response, answer.
/// </summary>
public interface IOfflineResponder
{
    string? TryRespond(OfflineChatRequest request);
}

/// <summary>The parts of a chat request that the offline client uses to build its answer.</summary>
public sealed record OfflineChatRequest(
    IReadOnlyList<ChatMessage> Messages,
    ChatOptions? Options,
    string Instructions,
    string UserText,
    string Fingerprint)
{
    /// <summary>The instructions followed by the user text.</summary>
    public string AllText => Instructions.Length == 0 ? UserText : $"{Instructions}\n{UserText}";

    /// <summary>The requested JSON format, when the caller asked for JSON output.</summary>
    public ChatResponseFormatJson? JsonFormat => Options?.ResponseFormat as ChatResponseFormatJson;

    public static OfflineChatRequest Create(IEnumerable<ChatMessage> messages, ChatOptions? options)
    {
        ArgumentNullException.ThrowIfNull(messages);

        var list = messages.ToList();
        var instructionParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(options?.Instructions))
        {
            instructionParts.Add(options.Instructions);
        }

        instructionParts.AddRange(list.Where(m => m.Role == ChatRole.System).Select(m => m.Text));

        var instructions = string.Join("\n", instructionParts.Where(p => p.Length > 0));
        var userText = string.Join("\n", list.Where(m => m.Role == ChatRole.User).Select(m => m.Text));
        var format = options?.ResponseFormat is ChatResponseFormatJson json
            ? $"json:{json.Schema?.GetRawText()}"
            : "text";

        return new OfflineChatRequest(list, options, instructions, userText, CreateFingerprint(instructions, list, format));
    }

    private static string CreateFingerprint(string instructions, IEnumerable<ChatMessage> messages, string format)
    {
        // FNV-1a over the request text gives the same identifier on every run and platform.
        var builder = new StringBuilder(instructions).Append('\n').Append(format);
        foreach (var message in messages)
        {
            builder.Append('\n').Append(message.Role.Value).Append(':').Append(message.Text);
        }

        var hash = 14695981039346656037UL;
        foreach (var b in Encoding.UTF8.GetBytes(builder.ToString()))
        {
            hash = (hash ^ b) * 1099511628211UL;
        }

        return $"offline-{hash:x16}";
    }
}
