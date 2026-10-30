using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace ContosoSupportCopilot.Infrastructure.AI;

// Serializer settings for model output. The same options build the JSON schema that
// GetResponseAsync<T> sends and read the answer, so the schema and the parser agree.
public static class StructuredOutputJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    private static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            NumberHandling = JsonNumberHandling.Strict,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            RespectNullableAnnotations = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { RequireEveryProperty } }
        };

        options.MakeReadOnly();
        return options;
    }

    // A missing property fails instead of silently keeping its default value.
    private static void RequireEveryProperty(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            property.IsRequired = true;
        }
    }
}
