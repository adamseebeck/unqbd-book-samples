using System.Text.Json;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Infrastructure.AI.Offline;

/// <summary>
/// Lets agents call their tools without a model. When a request offers tools and its instructions
/// name some of them, the first response asks for those calls, in the order the instructions name
/// them, with the user's message as every text argument. After the tools return, the response lists
/// their results. Requests without tools go to the inner offline client unchanged.
/// </summary>
public sealed class OfflineToolCallingChatClient : DelegatingChatClient
{
    private const int MaxResultLength = 240;
    private static readonly JsonSerializerOptions CompactJson = new(AIJsonUtilities.DefaultOptions) { WriteIndented = false };

    public OfflineToolCallingChatClient(IChatClient innerClient)
        : base(innerClient)
    {
    }

    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var list = messages.ToList();
        var request = OfflineChatRequest.Create(list, options);
        var tools = NamedTools(options, request.Instructions);
        if (tools.Count == 0)
        {
            return await base.GetResponseAsync(list, options, cancellationToken);
        }

        // Tool results after the last user message mean this turn's calls already ran.
        var lastUser = list.FindLastIndex(m => m.Role == ChatRole.User);
        var results = list.Skip(lastUser + 1).SelectMany(m => m.Contents.OfType<FunctionResultContent>()).ToList();
        var userText = lastUser >= 0 ? list[lastUser].Text : string.Empty;

        ChatMessage reply = results.Count == 0
            ? new ChatMessage(ChatRole.Assistant, tools.Select((t, i) => (AIContent)CreateCall(t, i, request.Fingerprint, userText)).ToList())
            : new ChatMessage(ChatRole.Assistant, Summarize(list, results));

        return new ChatResponse(reply)
        {
            ModelId = OfflineChatClient.ModelName,
            ResponseId = request.Fingerprint,
            FinishReason = results.Count == 0 ? ChatFinishReason.ToolCalls : ChatFinishReason.Stop,
            Usage = OfflineResponder.EstimateUsage(request, reply.Text.Length > 0 ? reply.Text : string.Join(" ", tools.Select(t => t.Name)))
        };
    }

    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var response = await GetResponseAsync(messages, options, cancellationToken);
        foreach (var update in response.ToChatResponseUpdates())
        {
            yield return update;
        }
    }

    private static List<AIFunctionDeclaration> NamedTools(ChatOptions? options, string instructions) =>
        options?.Tools?.OfType<AIFunctionDeclaration>()
            .Select(tool => (Tool: tool, Position: instructions.IndexOf(tool.Name, StringComparison.Ordinal)))
            .Where(x => x.Position >= 0)
            .OrderBy(x => x.Position)
            .Select(x => x.Tool)
            .ToList() ?? [];

    private static FunctionCallContent CreateCall(AIFunctionDeclaration tool, int index, string fingerprint, string userText)
    {
        // Text parameters get the user's message; a model would write its own search query here.
        var arguments = new Dictionary<string, object?>();
        if (tool.JsonSchema.ValueKind == JsonValueKind.Object && tool.JsonSchema.TryGetProperty("properties", out var properties))
        {
            foreach (var property in properties.EnumerateObject())
            {
                if (property.Value.TryGetProperty("type", out var type) && type.ValueKind == JsonValueKind.String && type.GetString() == "string")
                {
                    arguments[property.Name] = userText;
                }
            }
        }

        return new FunctionCallContent($"{fingerprint}-call-{index + 1}", tool.Name, arguments);
    }

    private static string Summarize(List<ChatMessage> messages, List<FunctionResultContent> results)
    {
        var names = new Dictionary<string, string>();
        foreach (var call in messages.SelectMany(m => m.Contents.OfType<FunctionCallContent>()))
        {
            names[call.CallId] = call.Name;
        }

        var parts = results.Select(r =>
        {
            var name = names.GetValueOrDefault(r.CallId, "tool");
            var value = r.Result is string text ? text : JsonSerializer.Serialize(r.Result, CompactJson);
            return $"{name} returned {(value.Length > MaxResultLength ? value[..MaxResultLength] + "..." : value)}";
        });
        return $"[Offline sample response] {string.Join("; ", parts)}.";
    }
}
