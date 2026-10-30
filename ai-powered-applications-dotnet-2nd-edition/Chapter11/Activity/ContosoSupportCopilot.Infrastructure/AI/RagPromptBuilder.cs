using System.Xml.Linq;
using ContosoSupportCopilot.Domain.Helpers;
using ContosoSupportCopilot.Domain.Models;
using ContosoSupportCopilot.Domain.Prompts;

namespace ContosoSupportCopilot.Infrastructure.AI;

public sealed class RagPromptBuilder
{
    public string BuildGroundedPrompt(
        PromptTemplate template,
        string ticketDescription,
        IReadOnlyList<CitationReference> sources)
    {
        // LINQ to XML escapes the content and every attribute value, so neither an article
        // nor the ticket can close its element or open a new one such as <system>.
        var knowledgeBase = new XElement("knowledge_base",
            sources.Select(source => new XElement("article",
                new XAttribute("id", source.ArticleId),
                new XAttribute("title", PromptTextHelper.XmlSafe(source.ArticleTitle)),
                new XAttribute("chunk", source.ChunkIndex),
                PromptTextHelper.XmlSafe(source.Content))));

        var ticket = new XElement("ticket_description", PromptTextHelper.XmlSafe(ticketDescription));

        return template.Render(new Dictionary<string, object>
        {
            ["knowledgeBase"] = knowledgeBase.ToString(),
            ["ticket"] = ticket.ToString()
        });
    }
}
