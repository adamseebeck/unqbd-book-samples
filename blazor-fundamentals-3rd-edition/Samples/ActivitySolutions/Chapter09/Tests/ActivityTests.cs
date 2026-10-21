using Bunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

public sealed class ActivityTests
{
    [Fact]
    public void ActivitySolutionHasTheExpectedVisibleBehavior()
    {
        using var context = new BunitContext();
        context.Services.AddSingleton<LibraryDesk.Client.Services.ICatalogQuery, LibraryDesk.Services.MemoryCatalogQuery>();
        var cut = context.Render<LibraryDesk.Client.Pages.ServicesDemo>();
        cut.WaitForAssertion(() => Assert.Equal(2, cut.FindAll("li").Count));
        Assert.Contains("Routing Field Notes", cut.Markup);
        Assert.Contains("State Field Notes", cut.Markup);
        Assert.DoesNotContain("Database Field Notes", cut.Markup);
    }
}
