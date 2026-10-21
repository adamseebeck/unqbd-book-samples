using Bunit;
using Xunit;
using Microsoft.AspNetCore.Components;
public sealed class InvestigationTests
{
    [Theory] [InlineData(2)] [InlineData(3)] [InlineData(4)]
    public void ScalarStepControlsOneClick(int step)
    {
        using var context = new BunitContext();
        context.Renderer.SetRendererInfo(new RendererInfo("Test", true));
        var cut = context.Render<LibraryDesk.Client.Components.ModeCounter>(p => p.Add(c => c.Step, step));
        cut.Find("button").Click();
        Assert.Contains($"Count: {step}", cut.Find("p").TextContent);
    }
}
