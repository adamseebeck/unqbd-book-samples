using Bunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

public sealed class ActivityTests
{
    [Fact]
    public void ActivitySolutionHasTheExpectedVisibleBehavior()
    {
        using var context = new BunitContext();
        var cut = context.Render<LibraryDesk.Client.Pages.BindingDemo>();
        Assert.Equal(3, cut.FindAll("li").Count);
        Assert.Equal("Matches: 3", cut.Find("#match-count").TextContent);
        cut.Find("input").Input("BLAZOR");
        Assert.Single(cut.FindAll("li"));
        Assert.Equal("Matches: 1", cut.Find("#match-count").TextContent);
        cut.Find("input").Input("absent");
        Assert.Empty(cut.FindAll("li"));
        Assert.Equal("Matches: 0", cut.Find("#match-count").TextContent);
    }
}
