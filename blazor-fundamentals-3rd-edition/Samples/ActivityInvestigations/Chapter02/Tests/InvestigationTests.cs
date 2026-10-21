using Bunit;
using Xunit;
public sealed class InvestigationTests
{
    [Fact] public void HomeContainsTheEditedSetupHeading()
    {
        using var context = new BunitContext();
        var cut = context.Render<LibraryDesk.Components.Pages.Home>();
        Assert.Equal("LibraryDesk setup verified", cut.Find("h1").TextContent);
    }
}
