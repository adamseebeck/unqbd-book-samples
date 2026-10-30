namespace ContosoSupportCopilot.Domain.Models;

// From Chapter 10: what the knowledge agent found for a ticket, handed to the drafting call so the reply
// takes its facts from cited articles. The Chapter 8 pipeline masked and sanitized the excerpts, and the
// answer is model output written from them.
public sealed record DraftingKnowledge(string Answer, IReadOnlyList<CitationReference> Sources);
