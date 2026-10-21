using Bunit;
using Xunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using LibraryDesk.Client.Models;
using LibraryDesk.Client.Services;
public sealed class InvestigationTests
{
    [Fact] public void ClearUpdatesTheInjectedStateAndItsVisibleStatus()
    {
        using var context = new BunitContext();
        context.Services.AddScoped<FavoritesState>();
        context.Services.AddSingleton<ICatalogQuery>(new Fixture());
        context.Renderer.SetRendererInfo(new RendererInfo("Test", true));
        var cut = context.Render<LibraryDesk.Client.Pages.Catalog>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("li").Count));
        cut.FindAll("li button")[0].Click(); cut.FindAll("li button")[1].Click();
        Assert.Contains("Favorites in this runtime: 2.", cut.Markup);
        cut.Find("#clear-catalog-favorites").Click(); cut.Find("#clear-catalog-favorites").Click();
        Assert.Empty(context.Services.GetRequiredService<FavoritesState>().Ids);
        Assert.Contains("Favorites in this runtime: 0.", cut.Markup);
    }
    private sealed class Fixture : ICatalogQuery
    {
        public Task<BookItem[]> GetAsync(CancellationToken token = default) => Task.FromResult(new[] { new BookItem(1, "C# Field Notes", 24), new BookItem(2, "Blazor Field Notes", 28) });
    }
}
