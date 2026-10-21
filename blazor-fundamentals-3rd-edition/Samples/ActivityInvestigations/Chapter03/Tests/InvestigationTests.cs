using Bunit;
using Xunit;
public sealed class InvestigationTests
{
    [Fact] public void EditedCounterRespondsAndNewInstanceStartsAtZero()
    {
        using var context = new BunitContext();
        var cut = context.Render<LibraryDesk.Client.Pages.Run>();
        Assert.Equal("My first server counter", cut.Find("h1").TextContent);
        cut.Find("button").Click(); cut.Find("button").Click();
        Assert.Equal("Current count: 2", cut.Find("#count").TextContent);
        cut.Dispose();
        var fresh = context.Render<LibraryDesk.Client.Pages.Run>();
        Assert.Equal("Current count: 0", fresh.Find("#count").TextContent);
    }
}
