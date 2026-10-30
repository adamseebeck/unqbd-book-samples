using System.Text;
using System.Xml;

namespace ContosoSupportCopilot.Domain.Helpers;

public static class PromptTextHelper
{
    // Escaping has limits: XML 1.0 cannot hold most control characters, such as the ESC of colored terminal output
    // or a form feed, nor a lone surrogate, and LINQ to XML's ToString throws on them. They carry no text, so they
    // are removed from every value before it goes into a prompt element or attribute.
    public static string XmlSafe(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(text.Length);
        for (var i = 0; i < text.Length; i++)
        {
            if (XmlConvert.IsXmlChar(text[i]))
            {
                builder.Append(text[i]);
            }
            else if (i + 1 < text.Length && XmlConvert.IsXmlSurrogatePair(text[i + 1], text[i]))
            {
                // An emoji or another character beyond U+FFFF: a high and a low surrogate that belong together
                builder.Append(text, i++, 2);
            }
        }

        return builder.ToString();
    }
}
