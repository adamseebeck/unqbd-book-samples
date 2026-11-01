using AngleSharp.Dom;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using LibraryDesk.Client.Pages;
using LibraryDesk.Client.Services;

public sealed class InvestigationTests
{
    [Fact]
    public void RemoveRefreshesTheSummaryInOneScopeOnly()
    {
        using var first = new BunitContext();
        using var second = new BunitContext();
        first.Services.AddScoped<FavoritesState>();
        second.Services.AddScoped<FavoritesState>();
        var cut = first.Render<FavoritesDemo>();
        var other = second.Render<FavoritesDemo>();

        Button(cut, "Favorite C#").Click();
        Button(cut, "Favorite Blazor").Click();
        cut.WaitForAssertion(() => Assert.Equal("Favorites: 2",
            cut.Find("#favorite-count").TextContent));

        Button(cut, "Remove C#").Click();
        Button(cut, "Remove C#").Click();
        cut.WaitForAssertion(() => Assert.Equal("Favorites: 1",
            cut.Find("#favorite-count").TextContent));
        Assert.Equal("Saved IDs: 2",
            cut.Find("#favorite-ids").TextContent);
        Assert.Equal("Favorites: 0",
            other.Find("#favorite-count").TextContent);
    }

    private static IElement Button(
        IRenderedComponent<FavoritesDemo> cut, string text) =>
        cut.FindAll("button")
            .Single(b => b.TextContent.Trim() == text);
}
