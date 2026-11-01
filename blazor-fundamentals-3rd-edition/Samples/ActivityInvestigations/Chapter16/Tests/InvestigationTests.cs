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
        // Use the parameter name so the unsolved project still builds.
        // The missing Step parameter is the runtime problem to investigate.
        var cut = context.Render<LibraryDesk.Client.Components.ModeCounter>(
            (RenderFragment)(builder =>
            {
                builder.OpenComponent<LibraryDesk.Client.Components.ModeCounter>(0);
                builder.AddAttribute(1, "Step", step);
                builder.CloseComponent();
            }));
        cut.Find("button").Click();
        Assert.Contains($"Count: {step}", cut.Find("p").TextContent);
    }
}
