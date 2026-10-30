using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

public sealed class InvestigationTests
{
    [Fact]
    public void MissingModuleReportsFailureAndDisablesActions()
    {
        using var context = new BunitContext();
        var runtime = new MissingModuleRuntime();
        context.Services.AddSingleton<IJSRuntime>(runtime);
        var cut = context.Render<LibraryDesk.Client.Pages.InteropDemo>();
        cut.WaitForAssertion(() => Assert.Contains(
            "Browser module unavailable. Reload to retry.", cut.Markup));
        Assert.Equal("./js/not-present.js", runtime.Path);
        Assert.All(cut.FindAll("button"),
            button => Assert.True(button.HasAttribute("disabled")));
    }

    // BL0016 asks app code to guard JS interop calls with try/catch.
    // This class stands in for the JavaScript runtime in the test.
#pragma warning disable BL0016
    private sealed class MissingModuleRuntime : IJSRuntime
    {
        public string? Path { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier, object?[]? args) =>
            InvokeCoreAsync<TValue>(args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier, CancellationToken token, object?[]? args) =>
            InvokeCoreAsync<TValue>(args);

        private ValueTask<TValue> InvokeCoreAsync<TValue>(object?[]? args)
        {
            Path = args?[0]?.ToString();
            return ValueTask.FromException<TValue>(
                new JSException("Module not found"));
        }
    }
#pragma warning restore BL0016
}
