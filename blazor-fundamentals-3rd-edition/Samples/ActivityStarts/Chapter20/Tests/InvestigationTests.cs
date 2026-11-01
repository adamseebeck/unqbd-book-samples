using Bunit;
using Xunit;
public sealed class InvestigationTests
{
    [Fact] public void ThreeTitleLimitPreservesStoredListAndReloadsIt()
    {
        using var context = new BunitContext();
        context.JSInterop.Setup<string?>("localStorage.getItem", "librarydesk.reading.v1").SetResult(null);
        context.JSInterop.SetupVoid("localStorage.setItem", _ => true).SetVoidResult();
        var cut = context.Render<LibraryDesk.Pwa.Pages.Home>();
        cut.WaitForAssertion(() => Assert.Contains("Loaded 0 local titles.", cut.Markup));
        foreach (var title in new[] { "One", "Two", "Three" })
        { cut.Find("input").Change(title); cut.Find("button").Click(); }
        cut.WaitForAssertion(() => Assert.Equal(3, cut.FindAll("li").Count));
        cut.Find("input").Change("Four"); cut.Find("button").Click();
        Assert.Contains("limited to 3 titles.", cut.Markup);
        var writes = context.JSInterop.Invocations.Where(x => x.Identifier == "localStorage.setItem").ToArray();
        Assert.Equal(3, writes.Length);
        var stored = Assert.IsType<string>(writes[^1].Arguments[1]);
        Assert.Equal(new[] { "One", "Two", "Three" }, System.Text.Json.JsonSerializer.Deserialize<string[]>(stored));
        using var reloaded = new BunitContext();
        reloaded.JSInterop.Setup<string?>("localStorage.getItem", "librarydesk.reading.v1").SetResult(stored);
        var fresh = reloaded.Render<LibraryDesk.Pwa.Pages.Home>();
        fresh.WaitForAssertion(() => Assert.Equal(new[] { "One", "Two", "Three" }, fresh.FindAll("li").Select(x => x.TextContent)));
    }
}
