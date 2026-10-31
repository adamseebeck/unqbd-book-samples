using Bunit;
using Xunit;

public sealed class InvestigationTests
{
    [Fact]
    public void ListAndMatchCountFollowEachKeystroke()
    {
        using var context = new BunitContext();
        var cut = context.Render<
            LibraryDesk.Client.Pages.BindingDemo>();
        Assert.Equal(3, cut.FindAll("li").Count);
        Assert.Equal("Matches: 3",
            cut.Find("#match-count").TextContent);

        // Input raises the input event, so this needs
        // @bind:event="oninput" on the text box.
        cut.Find("input").Input("BLAZOR");
        Assert.Single(cut.FindAll("li"));
        Assert.Equal("Matches: 1",
            cut.Find("#match-count").TextContent);

        cut.Find("input").Input("xyz");
        Assert.Empty(cut.FindAll("li"));
        Assert.Equal("Matches: 0",
            cut.Find("#match-count").TextContent);
    }
}
