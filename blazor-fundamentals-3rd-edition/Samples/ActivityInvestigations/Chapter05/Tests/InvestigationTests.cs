using Bunit;
using Xunit;
public sealed class InvestigationTests
{
    [Fact]
    public void OnChangeCommitsLaterThanInput()
    {
        using var context = new BunitContext();
        var cut = context.Render<LibraryDesk.Client.Pages.BindingDemo>();
        // This variant has no oninput handler. bUnit rejects that unsupported dispatch;
        // only a native browser edit can prove the before-blur behavior.
        Assert.Throws<MissingEventHandlerException>(() => cut.Find("input").Input("BLAZOR"));
        Assert.Equal(3, cut.FindAll("li").Count);
        cut.Find("input").Change("BLAZOR");
        Assert.Single(cut.FindAll("li"));
    }
}

