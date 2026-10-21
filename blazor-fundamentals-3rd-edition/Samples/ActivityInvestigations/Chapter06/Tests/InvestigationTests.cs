using Bunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using LibraryDesk.Client.Services;
public sealed class InvestigationTests
{
    [Fact] public void RemoveRefreshesSiblingAndDoesNotChangeAnotherScope()
    {
        using var first = new BunitContext(); using var second = new BunitContext();
        first.Services.AddScoped<FavoritesState>(); second.Services.AddScoped<FavoritesState>();
        var cut = first.Render<LibraryDesk.Client.Pages.FavoritesDemo>();
        var untouched = second.Render<LibraryDesk.Client.Pages.FavoritesDemo>();
        cut.FindAll("button")[0].Click(); cut.FindAll("button")[1].Click();
        cut.WaitForAssertion(() => Assert.Equal("Favorites: 2", cut.Find("#favorite-count").TextContent));
        cut.Find("#remove-csharp").Click(); cut.Find("#remove-csharp").Click();
        cut.WaitForAssertion(() => Assert.Equal("Favorites: 1", cut.Find("#favorite-count").TextContent));
        Assert.Equal(new[] { 2 }, first.Services.GetRequiredService<FavoritesState>().Ids);
        Assert.Equal("Favorites: 0", untouched.Find("#favorite-count").TextContent);
    }
}
