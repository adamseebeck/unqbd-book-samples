using Bunit;
using Xunit;
using Microsoft.JSInterop;
using Microsoft.Extensions.DependencyInjection;
public sealed class InvestigationTests
{
    [Fact]
    public void MissingModuleReportsFailureAndDisablesActions()
    {
        using var context = new BunitContext();
        var runtime = new MissingModuleRuntime();
        context.Services.AddSingleton<IJSRuntime>(runtime);
        var cut = context.Render<LibraryDesk.Client.Pages.InteropDemo>();
        cut.WaitForAssertion(() => Assert.Contains("Browser module unavailable. Reload to retry.", cut.Markup));
        Assert.Equal("./js/not-present.js", runtime.Path);
        Assert.All(cut.FindAll("button"), button => Assert.True(button.HasAttribute("disabled")));
    }
    private sealed class MissingModuleRuntime : IJSRuntime
    {
        public string? Path { get; private set; }
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeCoreAsync<TValue>(identifier, default, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken token, object?[]? args) => InvokeCoreAsync<TValue>(identifier, token, args);
        private ValueTask<TValue> InvokeCoreAsync<TValue>(string identifier, CancellationToken token, object?[]? args)
        {
            Path = args?[0]?.ToString();
            return ValueTask.FromException<TValue>(new JSException("Isolated missing module"));
        }
    }
}
