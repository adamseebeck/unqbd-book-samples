using Bunit;
using Xunit;
using Microsoft.Extensions.DependencyInjection;

public sealed class ActivityTests
{
    [Fact]
    public void ActivitySolutionHasTheExpectedVisibleBehavior()
    {
        using var context = new BunitContext();
        var cut = context.Render<LibraryDesk.Client.Pages.TemplatesDemo>();
        Assert.Contains("Select Database Field Notes", cut.Markup);
        Assert.Contains("$26.00", cut.Markup);
        cut.FindAll("button")[2].Click();
        Assert.Contains("Selected identifier: 3", cut.Markup);
    }
}
