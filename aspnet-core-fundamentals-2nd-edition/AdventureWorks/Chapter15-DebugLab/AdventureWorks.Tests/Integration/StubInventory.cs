using System.Net;
using System.Net.Http.Json;

namespace AdventureWorks.Tests.Integration;

// Answers InventoryClient's calls in place of InventoryService:
// seven of every SKU, or the status a test sets.
public sealed class StubInventory : HttpMessageHandler
{
    private int _calls;

    public HttpStatusCode Status { get; set; } =
        HttpStatusCode.OK;
    public int Calls => _calls;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _calls);
        var sku = request.RequestUri!.Segments[^1];
        var response = Status == HttpStatusCode.OK
            ? new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(
                    new { Sku = sku, Available = 7 })
            }
            : new HttpResponseMessage(Status);
        return Task.FromResult(response);
    }
}
