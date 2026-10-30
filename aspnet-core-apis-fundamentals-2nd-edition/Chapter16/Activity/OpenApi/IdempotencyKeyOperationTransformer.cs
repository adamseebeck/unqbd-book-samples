using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace WarehouseApi.OpenApi;

// The Idempotency-Key header binds to a nullable string, so the endpoints answer a missing key with their own
// 400; the generator would document such a header as optional, so mark it required wherever it appears
public sealed class IdempotencyKeyOperationTransformer : IOpenApiOperationTransformer
{
    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        foreach (var parameter in operation.Parameters?.OfType<OpenApiParameter>() ?? [])
        {
            if (parameter.In == ParameterLocation.Header && parameter.Name == "Idempotency-Key")
            {
                parameter.Required = true;
                parameter.Description = "A new random value for each operation; send the same value when you retry it.";
            }
        }

        return Task.CompletedTask;
    }
}
