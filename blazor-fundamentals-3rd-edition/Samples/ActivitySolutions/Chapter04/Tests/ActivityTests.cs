using Bunit;
using Xunit;

public sealed class ActivityTests
{
    [Fact]
    public void ThirdNoteTogglesOnItsOwn()
    {
        using var context = new BunitContext();
        var cut = context.Render<
            LibraryDesk.Client.Pages.ComponentsDemo>();
        var buttons = cut.FindAll("button");
        Assert.Equal(3, buttons.Count);
        Assert.Equal("Tap to read", cut.FindAll("p")[2].TextContent);

        cut.FindAll("button")[1].Click();
        cut.FindAll("button")[1].Click();
        cut.FindAll("button")[2].Click();

        var notes = cut.FindAll("p");
        Assert.Equal("Details hidden", notes[0].TextContent);
        Assert.Equal("Details hidden", notes[1].TextContent);
        Assert.Equal("Database Field Notes is available.",
            notes[2].TextContent);
    }
}
