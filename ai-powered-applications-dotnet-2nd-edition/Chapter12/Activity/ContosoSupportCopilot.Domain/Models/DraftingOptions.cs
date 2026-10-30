namespace ContosoSupportCopilot.Domain.Models;

public record DraftingOptions
{
    public int? MaxOutputTokens { get; init; } = 2000;
    public float? Temperature { get; init; }

    // From Chapter 10: the knowledge agent's cited answer. Null drafts from the ticket alone, as in Chapter 3.
    public DraftingKnowledge? Knowledge { get; init; }
}
