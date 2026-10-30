using System.Net;
using Bunit;
using LibraryDesk.Client.Components;
using LibraryDesk.Client.Models;
using LibraryDesk.Client.Pages;
using LibraryDesk.Client.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class ComponentTests
{
    [Fact]
    public void CounterRespondsToClicks()
    {
        using var context = new BunitContext();
        context.Renderer.SetRendererInfo(
            new RendererInfo("Server", true));
        var cut = context.Render<Run>();
        cut.Find("button").Click();
        Assert.Contains("Current count: 1",
            cut.Find("#count").TextContent);
    }

    [Fact]
    public void ToggleStateIsLocalToEachComponent()
    {
        using var context = new BunitContext();
        var cut = context.Render<ComponentsDemo>();
        cut.FindAll("button")[0].Click();
        Assert.Contains("C# Field Notes is available.",
            cut.Markup);
        Assert.Contains("Details hidden", cut.Markup);
        var first = cut.FindAll("button")[0];
        var second = cut.FindAll("button")[1];
        Assert.Equal("true",
            first.GetAttribute("aria-expanded"));
        Assert.Equal("false",
            second.GetAttribute("aria-expanded"));
    }

    [Theory]
    [InlineData("BLAZOR", 1)]
    [InlineData("unmatched", 0)]
    [InlineData("", 3)]
    public void BindingFiltersOnInput(string query, int expected)
    {
        using var context = new BunitContext();
        var cut = context.Render<BindingDemo>();
        cut.Find("input").Input(query);
        Assert.Equal(expected, cut.FindAll("li").Count);
    }

    [Fact]
    public void FavoritesRefreshSiblingsAndIgnoreDuplicates()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<FavoritesState>();
        var cut = context.Render<FavoritesDemo>();
        string Count() => cut.Find("#favorite-count").TextContent;
        cut.FindAll("button")[0].Click();
        cut.FindAll("button")[0].Click();
        cut.WaitForAssertion(
            () => Assert.Equal("Favorites: 1", Count()));
        cut.FindAll("button")[1].Click();
        cut.WaitForAssertion(
            () => Assert.Equal("Favorites: 2", Count()));
        cut.FindAll("button")[2].Click();
        cut.WaitForAssertion(
            () => Assert.Equal("Favorites: 0", Count()));
    }

    [Fact]
    public void SeparateScopesOwnSeparateFavorites()
    {
        using var first = new BunitContext();
        using var second = new BunitContext();
        first.Services.AddScoped<FavoritesState>();
        second.Services.AddScoped<FavoritesState>();
        var firstPage = first.Render<FavoritesDemo>();
        var secondPage = second.Render<FavoritesDemo>();
        firstPage.FindAll("button")[0].Click();
        firstPage.WaitForAssertion(() => Assert.Equal(
            "Favorites: 1",
            firstPage.Find("#favorite-count").TextContent));
        Assert.Equal("Favorites: 0",
            secondPage.Find("#favorite-count").TextContent);
    }

    [Fact]
    public void InvalidFeedbackDoesNotReportSuccess()
    {
        using var context = new BunitContext();
        var cut = context.Render<FeedbackForm>();
        cut.Find("form").Submit();
        Assert.NotEmpty(cut.FindAll(".validation-message"));
        Assert.Contains("Nothing saved.", cut.Markup);
    }

    [Theory]
    [InlineData("invalid-address", "A valid length message")]
    [InlineData("ada@example.test", "four")]
    public void InvalidEmailOrShortMessageDoesNotReportSuccess(
        string email, string message)
    {
        using var context = new BunitContext();
        var cut = context.Render<FeedbackForm>();
        cut.Find("#feedback-name").Change("Ada");
        cut.Find("#feedback-email").Change(email);
        cut.Find("#feedback-message").Change(message);
        cut.Find("form").Submit();
        Assert.NotEmpty(cut.FindAll(".validation-message"));
        Assert.Contains("Nothing saved.", cut.Markup);
    }

    [Fact]
    public void ValidFeedbackShowsLocalOutcome()
    {
        using var context = new BunitContext();
        var cut = context.Render<FeedbackForm>();
        cut.Find("#feedback-name").Change("Ada");
        cut.Find("#feedback-email").Change("ada@example.test");
        cut.Find("#feedback-message")
            .Change("Please add a routing example.");
        cut.Find("form").Submit();
        Assert.Contains("accepted locally for Ada", cut.Markup);
        Assert.Contains("No account was created", cut.Markup);
    }

    [Fact]
    public void TypedCallbackCarriesBookIdentifier()
    {
        using var context = new BunitContext();
        var cut = context.Render<TemplatesDemo>();
        Assert.Contains("Select C# Field Notes", cut.Markup);
        Assert.Contains("Select Blazor Field Notes", cut.Markup);
        cut.FindAll("button")[1].Click();
        Assert.Contains("Selected identifier: 2", cut.Markup);
    }

    [Fact]
    public void ApiFailureBecomesRecoverableUi()
    {
        using var context = new BunitContext();
        var handler = new NotFoundHandler();
        using var http = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        context.Services.AddSingleton<ICatalogQuery>(
            new HttpCatalogQuery(http));
        var cut = context.Render<ApiDemo>();
        cut.Find("button").Click();
        cut.WaitForAssertion(() => Assert.Contains(
            "catalog is unavailable", cut.Markup));
        Assert.False(cut.Find("button").HasAttribute("disabled"));
    }

    [Fact]
    public void CatalogLoadsAndFiltersItsInjectedData()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<ICatalogQuery>(
            new FixedCatalog());
        context.Services.AddScoped<FavoritesState>();
        context.Renderer.SetRendererInfo(
            new RendererInfo("WebAssembly", true));
        var cut = context.Render<Catalog>();
        cut.WaitForAssertion(
            () => Assert.Equal(2, cut.FindAll("li").Count));
        cut.Find("input").Input("Blazor");
        Assert.Single(cut.FindAll("li"));
        cut.Find("li a").MarkupMatches(
            "<a href=\"books/2\">Blazor Field Notes</a>");
        cut.Find("button").Click();
        Assert.Contains("Favorites in this runtime: 1",
            cut.Markup);
        cut.Find("input").Input("Cooking");
        Assert.Empty(cut.FindAll("li"));
        Assert.Contains("No matching titles.", cut.Markup);
    }

    private sealed class NotFoundHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken token) =>
            Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private sealed class FixedCatalog : ICatalogQuery
    {
        public Task<BookItem[]> GetAsync(
            CancellationToken token = default) =>
            Task.FromResult(new[]
            {
                new BookItem(1, "C# Field Notes", 24),
                new BookItem(2, "Blazor Field Notes", 28)
            });
    }
}
