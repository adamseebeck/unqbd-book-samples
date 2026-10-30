using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ContosoSupportCopilot.Infrastructure.AI.Offline;

/// <summary>
/// Produces JSON answers for offline requests: an instance that satisfies a JSON schema,
/// or the JSON example or field list found in the instructions, filled with sample values.
/// </summary>
internal static partial class OfflineJson
{
    private const int MaxDepth = 8;

    public static string FromSchema(JsonElement schema, OfflineClassification c) =>
        (Generate(schema, schema, propertyName: null, c, depth: 0) ?? new JsonObject()).ToJsonString();

    public static string? FromInstructions(string text, OfflineClassification c)
    {
        foreach (var candidate in JsonObjectCandidates(text))
        {
            if (TryParseObject(candidate) is { Count: > 0 } example)
            {
                return Fill(example, c).ToJsonString();
            }
        }

        // "Return valid JSON with fields: Summary, Impact, ReproductionSteps."
        var match = FieldList().Match(text);
        if (match.Success)
        {
            var result = new JsonObject();
            foreach (var name in Regex.Split(match.Groups["fields"].Value, @"\s*(?:,\s*and\s+|,|\s+and\s+)\s*"))
            {
                if (name.Length > 0)
                {
                    result[name] = StringFor(name, c);
                }
            }

            return result.ToJsonString();
        }

        return null;
    }

    private static JsonNode? Generate(JsonElement node, JsonElement root, string? propertyName, OfflineClassification c, int depth)
    {
        if (depth > MaxDepth)
        {
            return null;
        }

        if (node.ValueKind == JsonValueKind.True)
        {
            return StringFor(propertyName, c);
        }

        if (node.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        if (node.TryGetProperty("$ref", out var reference) && Resolve(root, reference.GetString()) is { } target)
        {
            return Generate(target, root, propertyName, c, depth + 1);
        }

        if (node.TryGetProperty("const", out var constant))
        {
            return JsonNode.Parse(constant.GetRawText());
        }

        if (node.TryGetProperty("enum", out var values) && values.ValueKind == JsonValueKind.Array && values.GetArrayLength() > 0)
        {
            return PickEnum(values, propertyName, c);
        }

        foreach (var keyword in new[] { "anyOf", "oneOf", "allOf" })
        {
            if (node.TryGetProperty(keyword, out var choices) && choices.ValueKind == JsonValueKind.Array)
            {
                foreach (var choice in choices.EnumerateArray())
                {
                    if (PrimaryType(choice) != "null")
                    {
                        return Generate(choice, root, propertyName, c, depth + 1);
                    }
                }
            }
        }

        switch (PrimaryType(node))
        {
            case "object" or null when node.TryGetProperty("properties", out var properties):
                var obj = new JsonObject();
                foreach (var property in properties.EnumerateObject())
                {
                    obj[property.Name] = Generate(property.Value, root, property.Name, c, depth + 1);
                }

                return obj;
            case "object":
                return new JsonObject();
            case "array":
                var array = new JsonArray();
                var count = node.TryGetProperty("minItems", out var min) && min.TryGetInt32(out var minItems) ? Math.Max(1, minItems) : 1;
                if (node.TryGetProperty("maxItems", out var max) && max.TryGetInt32(out var maxItems))
                {
                    count = Math.Min(count, maxItems);
                }

                if (node.TryGetProperty("items", out var items))
                {
                    for (var i = 0; i < count; i++)
                    {
                        array.Add(Generate(items, root, propertyName, c, depth + 1));
                    }
                }

                return array;
            case "integer":
                return JsonValue.Create((long)Math.Round(NumberFor(propertyName, node, c, integer: true)));
            case "number":
                return JsonValue.Create(NumberFor(propertyName, node, c, integer: false));
            case "boolean":
                return JsonValue.Create(false);
            case "null":
                return null;
            default:
                return node.TryGetProperty("format", out var format) && format.GetString() == "date-time"
                    ? JsonValue.Create("2026-01-01T00:00:00Z")
                    : StringFor(propertyName, c);
        }
    }

    private static JsonObject Fill(JsonObject example, OfflineClassification c)
    {
        var result = new JsonObject();
        foreach (var (name, value) in example)
        {
            result[name] = value switch
            {
                JsonObject nested => Fill(nested, c),
                JsonArray => new JsonArray(),
                JsonValue v when v.GetValueKind() == JsonValueKind.Number => JsonValue.Create(NumberFor(name, default, c, integer: false)),
                JsonValue v when v.GetValueKind() is JsonValueKind.True or JsonValueKind.False => JsonValue.Create(false),
                _ => StringFor(name, c)
            };
        }

        return result;
    }

    private static JsonNode StringFor(string? propertyName, OfflineClassification c)
    {
        var name = Normalize(propertyName);
        string value = name switch
        {
            _ when name.Contains("intent") => c.Intent,
            _ when name.Contains("urgency") || name.Contains("priority") || name.Contains("severity") => c.Urgency,
            _ when name.Contains("productarea") || name.Contains("category") || name == "area" => c.ProductArea,
            _ when name.Contains("summary") => $"Offline sample summary of {IndefiniteArticle(c.ProductArea)} {c.ProductArea} {c.Intent} request.",
            _ when name.Contains("impact") => $"{c.Urgency} urgency for the customer's {c.ProductArea} work.",
            _ when name.Contains("step") => "1. Repeat the customer's action. 2. Compare the result with the expected behavior.",
            _ when name.Contains("reason") || name.Contains("explanation") => "Offline sample: classified from keywords in the request.",
            _ when name.Contains("title") || name.Contains("subject") => $"{c.ProductArea} {c.Intent}",
            _ when name.Contains("reply") || name.Contains("answer") || name.Contains("draft") || name.Contains("response") =>
                $"Offline sample reply: the next step is to {OfflineResponder.NextStep(c.ProductArea)}.",
            _ => "Offline sample value"
        };

        return JsonValue.Create(value);
    }

    private static double NumberFor(string? propertyName, JsonElement schema, OfflineClassification c, bool integer)
    {
        var name = Normalize(propertyName);
        var value = name.Contains("confidence") || name.Contains("score") || name.Contains("probability")
            ? c.Confidence
            : 1;

        if (schema.ValueKind == JsonValueKind.Object)
        {
            if (schema.TryGetProperty("minimum", out var minimum) && minimum.TryGetDouble(out var low))
            {
                value = Math.Max(value, low);
            }

            if (schema.TryGetProperty("maximum", out var maximum) && maximum.TryGetDouble(out var high))
            {
                value = Math.Min(value, high);
            }
        }

        return integer ? Math.Max(Math.Round(value), 0) : value;
    }

    private static JsonNode? PickEnum(JsonElement values, string? propertyName, OfflineClassification c)
    {
        var preferred = StringFor(propertyName, c).GetValue<string>();
        foreach (var value in values.EnumerateArray())
        {
            if (value.ValueKind == JsonValueKind.String && string.Equals(value.GetString(), preferred, StringComparison.OrdinalIgnoreCase))
            {
                return JsonNode.Parse(value.GetRawText());
            }
        }

        return JsonNode.Parse(values[0].GetRawText());
    }

    private static string? PrimaryType(JsonElement node)
    {
        if (!node.TryGetProperty("type", out var type))
        {
            return null;
        }

        if (type.ValueKind == JsonValueKind.String)
        {
            return type.GetString();
        }

        return type.ValueKind == JsonValueKind.Array
            ? type.EnumerateArray().Select(t => t.GetString()).FirstOrDefault(t => t != "null") ?? "null"
            : null;
    }

    private static JsonElement? Resolve(JsonElement root, string? reference)
    {
        if (reference is null || !reference.StartsWith('#'))
        {
            return null;
        }

        var current = root;
        foreach (var segment in reference.TrimStart('#').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            var key = segment.Replace("~1", "/").Replace("~0", "~");
            if (current.ValueKind != JsonValueKind.Object || !current.TryGetProperty(key, out var next))
            {
                return null;
            }

            current = next;
        }

        return current;
    }

    private static IEnumerable<string> JsonObjectCandidates(string text)
    {
        for (var start = text.IndexOf('{'); start >= 0; start = text.IndexOf('{', start + 1))
        {
            var depth = 0;
            var inString = false;
            for (var i = start; i < text.Length; i++)
            {
                var ch = text[i];
                if (inString)
                {
                    if (ch == '\\')
                    {
                        i++;
                    }
                    else if (ch == '"')
                    {
                        inString = false;
                    }
                }
                else if (ch == '"')
                {
                    inString = true;
                }
                else if (ch == '{')
                {
                    depth++;
                }
                else if (ch == '}' && --depth == 0)
                {
                    yield return text[start..(i + 1)];
                    break;
                }
            }
        }
    }

    private static JsonObject? TryParseObject(string candidate)
    {
        try
        {
            return JsonNode.Parse(candidate) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    // "an API request", "a Payments request"
    private static string IndefiniteArticle(string word) =>
        word.Length > 0 && "AEIOUaeiou".Contains(word[0]) ? "an" : "a";

    private static string Normalize(string? name) =>
        new((name ?? string.Empty).Where(char.IsLetterOrDigit).Select(char.ToLowerInvariant).ToArray());

    [GeneratedRegex(@"fields?\s*:\s*(?<fields>[A-Za-z_][A-Za-z0-9_]*(?:\s*(?:,\s*and\s+|,|\s+and\s+)\s*[A-Za-z_][A-Za-z0-9_]*)+)", RegexOptions.IgnoreCase)]
    private static partial Regex FieldList();
}
