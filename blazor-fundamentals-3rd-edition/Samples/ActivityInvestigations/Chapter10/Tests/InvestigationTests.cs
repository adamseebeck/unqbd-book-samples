using Bunit;
using Xunit;
using System.Net;
using Microsoft.Extensions.DependencyInjection;
using LibraryDesk.Client.Services;
public sealed class InvestigationTests
{
    [Fact]
    public void ChangedQueryCallsMissingPathAndLeavesRetryAvailable()
    {
        using var context = new BunitContext();
        var handler = new MissingHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("http://localhost/") };
        context.Services.AddSingleton<ICatalogQuery>(new HttpCatalogQuery(http));
        var cut = context.Render<LibraryDesk.Client.Pages.ApiDemo>();
        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Contains("catalog is unavailable", cut.Markup));
        Assert.Equal("/api/not-present", handler.Path);
        Assert.False(cut.Find("button").HasAttribute("disabled"));
    }
    private sealed class MissingHandler : HttpMessageHandler
    {
        public string? Path { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            Path = request.RequestUri?.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }
}
