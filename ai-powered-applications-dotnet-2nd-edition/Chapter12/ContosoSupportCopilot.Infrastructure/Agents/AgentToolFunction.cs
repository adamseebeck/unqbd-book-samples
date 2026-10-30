using System.Text.Json;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.AI;
using Microsoft.Extensions.AI;

namespace ContosoSupportCopilot.Infrastructure.Agents;

// One call of a tool during an agent run: the arguments the model sent and what the tool returned.
public sealed record AgentToolCall(object? Input, AgentToolResult Result);

// The tool boundary: an IAgentTool exposed to the model as an AIFunction. The model supplies only
// the arguments in the tool's InputType; the ticket, conversation and requester come from the context.
public sealed class AgentToolFunction : AIFunction
{
    private readonly IAgentTool _tool;
    private readonly AgentToolContext _context;
    private readonly List<AgentToolCall> _calls = [];

    public AgentToolFunction(IAgentTool tool, AgentToolContext context)
    {
        _tool = tool;
        _context = context;

        // Strict options: a missing or unexpected argument fails instead of being guessed.
        JsonSchema = AIJsonUtilities.CreateJsonSchema(tool.InputType, serializerOptions: StructuredOutputJson.Options);
    }

    public override string Name => _tool.Name;
    public override string Description => _tool.Description;
    public override JsonElement JsonSchema { get; }

    // Every call of this run, so the agent reads typed values instead of parsing the model's text.
    public IReadOnlyList<AgentToolCall> Calls => _calls;

    protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
    {
        object input;
        try
        {
            var json = JsonSerializer.SerializeToElement(arguments.ToDictionary(a => a.Key, a => a.Value), AIJsonUtilities.DefaultOptions);
            input = json.Deserialize(_tool.InputType, StructuredOutputJson.Options)
                ?? throw new JsonException("The arguments are empty.");
        }
        catch (JsonException ex)
        {
            var invalid = AgentToolResult.Failure(AgentFailureMode.Unrecoverable, $"Invalid arguments for {_tool.Name}: {ex.Message}");
            _calls.Add(new AgentToolCall(null, invalid));
            return new { error = invalid.ErrorMessage };
        }

        var result = await _tool.ExecuteAsync(input, _context, cancellationToken);
        _calls.Add(new AgentToolCall(input, result));

        // The model sees a failure as data and can say so; the agent reads the failure mode from Calls.
        return result.IsSuccess ? result.ModelOutput ?? result.Value : new { error = result.ErrorMessage };
    }
}
