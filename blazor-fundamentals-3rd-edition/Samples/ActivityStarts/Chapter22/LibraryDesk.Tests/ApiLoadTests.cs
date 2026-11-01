using Bunit;
using LibraryDesk.Client.Models;
using LibraryDesk.Client.Pages;
using LibraryDesk.Client.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class ApiLoadTests
{
    [Fact]
    public async Task SecondClickWaitsAndFailureAllowsRetry()
    {
        using var context = new BunitContext();
        var query = new ControlledCatalog();
        context.Services.AddSingleton<ICatalogQuery>(query);
        var cut = context.Render<ApiDemo>();
        var first = cut.Find("button")
            .TriggerEventAsync("onclick", new MouseEventArgs());
        cut.WaitForAssertion(() => Assert.Equal(1, query.Calls));
        // Dispatch the already-queued handler even though
        // the rendered button is disabled.
        var second = cut.Find("button")
            .TriggerEventAsync("onclick", new MouseEventArgs());
        try
        {
            await cut.InvokeAsync(() => { });
            Assert.Equal(1, query.Calls);
            Assert.True(
                cut.Find("button").HasAttribute("disabled"));
            Assert.Contains("Loading...", cut.Markup);
        }
        finally
        {
            query.FailPending();
            await Task.WhenAll(first, second).WaitAsync(
                TimeSpan.FromSeconds(5),
                Xunit.TestContext.Current.CancellationToken);
        }
        cut.WaitForAssertion(() => Assert.Contains(
            "The catalog is unavailable. Try again.",
            cut.Markup));
        Assert.False(cut.Find("button").HasAttribute("disabled"));
        await cut.Find("button").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(
            () => Assert.Equal(3, cut.FindAll("li").Count));
        Assert.Equal(2, query.Calls);
        Assert.False(cut.Find("button").HasAttribute("disabled"));
    }

    private sealed class ControlledCatalog : ICatalogQuery
    {
        private readonly List<TaskCompletionSource<BookItem[]>>
            pending = [];
        private bool released;
        public int Calls { get; private set; }

        public Task<BookItem[]> GetAsync(
            CancellationToken token = default)
        {
            Calls++;
            if (released)
            {
                return Task.FromResult(new[]
                {
                    new BookItem(1, "C# Field Notes", 24),
                    new BookItem(2, "Blazor Field Notes", 28),
                    new BookItem(3, "Database Field Notes", 26)
                });
            }
            var request = new TaskCompletionSource<BookItem[]>(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);
            pending.Add(request);
            return request.Task;
        }

        public void FailPending()
        {
            released = true;
            foreach (var request in pending)
            {
                request.TrySetException(new HttpRequestException(
                    "Controlled first-read failure."));
            }
        }
    }
}
