using Bunit;
using Xunit;

public sealed class ActivityTests
{
    [Fact]
    public void ThirdBookShowsItsPriceAndSelects()
    {
        using var context = new BunitContext();
        var cut = context.Render<
            LibraryDesk.Client.Pages.TemplatesDemo>();
        var rows = cut.FindAll("li")
            .Select(li => string.Join(" ", li.TextContent.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries)))
            .ToArray();
        Assert.Equal(
            [
                "$24.00 Select C# Field Notes",
                "$28.00 Select Blazor Field Notes",
                "$26.00 Select Database Field Notes"
            ],
            rows);

        cut.FindAll("li button")[2].Click();
        Assert.Contains("Selected identifier: 3", cut.Markup);
    }
}
