using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Api;

// The Agent Framework types on their own, before the domain wrapper: an agent over the
// application's IChatClient, a tool made from a method, a session, and session serialization.
public static class AgentPrimitivesExample
{
    public static async Task RunAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        // The IChatClient that Chapter 2 registers for every provider.
        IChatClient chatClient = services.GetRequiredService<IChatClient>();

        // AIFunctionFactory turns a method into a tool: name, description and a JSON schema for its parameters.
        AIFunction getSupportPlan = AIFunctionFactory.Create(
            (string customerName) => customerName.Contains("Fabrikam", StringComparison.OrdinalIgnoreCase) ? "Enterprise" : "Basic",
            name: "get_support_plan",
            description: "Returns the support plan of a Contoso customer.");

        ChatClientAgent agent = chatClient.AsAIAgent(
            instructions: "You answer questions about Contoso customers. Call get_support_plan for questions about a plan.",
            name: "PlanAgent",
            tools: [getSupportPlan]);

        // A session holds the conversation; pass the same session to every turn.
        AgentSession session = await agent.CreateSessionAsync(cancellationToken);
        AgentResponse response = await agent.RunAsync("Which support plan does Fabrikam have?", session, cancellationToken: cancellationToken);
        Console.WriteLine($"PlanAgent: {response.Text}");
        Console.WriteLine($"Usage: {response.Usage?.InputTokenCount} input and {response.Usage?.OutputTokenCount} output tokens");

        // Streaming yields AgentResponseUpdate items on the same session.
        var updates = 0;
        await foreach (AgentResponseUpdate update in agent.RunStreamingAsync("And Tailspin Toys?", session, cancellationToken: cancellationToken))
        {
            updates++;
        }

        // The session serializes to JSON, so a later request, or another server, can continue it.
        JsonElement state = await agent.SerializeSessionAsync(session, cancellationToken: cancellationToken);
        AgentSession restored = await agent.DeserializeSessionAsync(state, cancellationToken: cancellationToken);
        var messages = agent.ChatHistoryProvider is InMemoryChatHistoryProvider history ? history.GetMessages(restored).Count : 0;
        Console.WriteLine($"Streamed {updates} updates; session saved as {state.GetRawText().Length} characters of JSON and restored with {messages} messages");
    }
}
