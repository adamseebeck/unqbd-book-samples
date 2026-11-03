using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MauiInventory.Core;
[JsonSerializable(typeof(Product[]))]
public partial class InventoryJsonContext : JsonSerializerContext
{
}

public sealed class CatalogClient(HttpClient httpClient, Uri endpoint)
{
    private const int MaxBodyBytes = 1_048_576;
    public async Task<IReadOnlyList<Product>> GetAsync(CancellationToken cancellationToken = default)
    {
        if (endpoint.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Catalog endpoint must use HTTPS.");
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(15));
        using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
        using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MaxBodyBytes)
            throw new InvalidDataException("Catalog response is too large.");
        await using var body = await response.Content.ReadAsStreamAsync(deadline.Token).ConfigureAwait(false);
        using var buffer = new MemoryStream();
        byte[] chunk = new byte[8192];
        int read;
        while ((read = await body.ReadAsync(chunk, deadline.Token).ConfigureAwait(false)) > 0)
        {
            if (buffer.Length + read > MaxBodyBytes)
                throw new InvalidDataException("Catalog response is too large.");
            buffer.Write(chunk, 0, read);
        }

        Product[] products;
        try
        {
            products = JsonSerializer.Deserialize(buffer.ToArray(), InventoryJsonContext.Default.ProductArray) ?? throw new InvalidDataException("Catalog response was null.");
            foreach (var p in products)
            {
                if (p is null)
                    throw new InvalidDataException("Catalog contains a null product.");
                p.Validate();
            }
        }
        catch (JsonException error)
        {
            throw new InvalidDataException("Catalog JSON has an invalid shape.", error);
        }
        catch (ArgumentException error)
        {
            throw new InvalidDataException("Catalog product fields are invalid.", error);
        }

        if (products.Select(p => p.Id).Distinct().Count() != products.Length || products.Any(p => p.Id <= 0))
            throw new InvalidDataException("Catalog identifiers must be unique and positive.");
        return products;
    }
}

// The installed sample uses an in-process response, so learning does not write to a live API.
public sealed class DemoCatalogHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("[{\"Id\":10,\"Sku\":\"DESK\",\"Name\":\"Standing desk\",\"Quantity\":4,\"UnitPriceCents\":24999}]") });
    }
}
