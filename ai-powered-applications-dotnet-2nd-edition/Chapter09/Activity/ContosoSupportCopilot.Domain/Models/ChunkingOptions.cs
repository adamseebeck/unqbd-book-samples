namespace ContosoSupportCopilot.Domain.Models;

public record ChunkingOptions
{
    // Characters, not tokens: about 512 characters is roughly 128 tokens of English text.
    public int MaxChunkSize { get; init; } = 512;

    // Characters repeated from the end of one chunk at the start of the next.
    public int OverlapSize { get; init; } = 50;

    // True keeps a paragraph in one chunk whenever it fits; false fills each chunk with sentences.
    public bool PreserveParagraphBoundaries { get; init; } = true;
}
