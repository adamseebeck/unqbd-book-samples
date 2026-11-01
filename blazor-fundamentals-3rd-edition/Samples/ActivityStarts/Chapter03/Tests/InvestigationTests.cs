using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Xunit;
using LibraryDesk.Client.Pages;

public sealed class InvestigationTests
{
    [Fact]
    public void ResetSetsTheCountBackToZero()
    {
        using var context = new BunitContext();
        context.Renderer.SetRendererInfo(
            new RendererInfo("Server", true));
        var cut = context.Render<Run>();
        Button(cut, "Add one").Click();
        Button(cut, "Add one").Click();
        Assert.Equal("Current count: 2",
            cut.Find("#count").TextContent);

        Button(cut, "Reset").Click();
        Assert.Equal("Current count: 0",
            cut.Find("#count").TextContent);
    }

    private static IElement Button(
        IRenderedComponent<Run> cut, string text) =>
        cut.FindAll("button")
            .Single(b => b.TextContent.Trim() == text);
}
