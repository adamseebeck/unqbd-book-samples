namespace ContosoSupportCopilot.Domain.Models;

public record DraftingOptions
{
    public int? MaxOutputTokens { get; init; } = 2000;
    public float? Temperature { get; init; }
}
