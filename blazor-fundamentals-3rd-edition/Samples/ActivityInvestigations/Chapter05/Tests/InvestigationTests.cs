using Bunit;
using Xunit;
public sealed class InvestigationTests
{
    [Fact]
    public void OnChangeCommitsLaterThanInput()
    {
        using var context = new BunitContext();
        var cut = context.Render<LibraryDesk.Client.Pages.BindingDemo>();
        // The input has no oninput handler, so bUnit refuses an input
        // event; the list changes only on the change event.
        Assert.Throws<MissingEventHandlerException>(() => cut.Find("input").Input("BLAZOR"));
        Assert.Equal(3, cut.FindAll("li").Count);
        cut.Find("input").Change("BLAZOR");
        Assert.Single(cut.FindAll("li"));
    }
}

