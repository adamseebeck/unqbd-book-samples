using Bunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

public sealed class ActivityTests
{
    [Fact]
    public void ActivitySolutionHasTheExpectedVisibleBehavior()
    {
        using var context = new BunitContext();
        var cut = context.Render<LibraryDesk.Client.Pages.ComponentsDemo>();
        Assert.Equal(3, cut.FindAll("button").Count);
        cut.FindAll("button")[1].Click();
        Assert.Contains("Blazor Field Notes is available.", cut.Markup);
        Assert.Equal("Details hidden", cut.FindAll("p")[0].TextContent);
        Assert.Equal("Database details hidden", cut.FindAll("p")[2].TextContent);
        cut.FindAll("button")[1].Click();
        Assert.Equal("Details hidden", cut.FindAll("p")[1].TextContent);
        Assert.Equal("Database details hidden", cut.FindAll("p")[2].TextContent);
    }
}
