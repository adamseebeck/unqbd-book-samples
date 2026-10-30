using Bunit;
using LibraryDesk.Client.Pages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

public sealed class InteropLifetimeTests
{
    [Fact]
    public async Task ImportFinishingAfterDisposalReleasesItsUnownedModule()
    {
        using var context = new BunitContext();
        var runtime = new PendingImport();
        context.Services.AddSingleton<IJSRuntime>(runtime);
        var cut = context.Render<ImportObserver>();
        Assert.Equal(1, runtime.Imports);
        var firstRender = cut.Instance.FirstAfterRender
            ?? throw new InvalidOperationException("The actual first lifecycle task was not captured.");
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());
        var module = new Module();
        runtime.Completion.SetResult(module);
        await firstRender;
        Assert.Equal(1, module.Disposals);
        Assert.Equal(0, module.Invocations);
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());
        Assert.Equal(1, module.Disposals);
    }

    [Theory] [InlineData(false)] [InlineData(true)]
    public async Task NormalDisposalInvalidatesTheDotNetReferenceAndToleratesDisconnect(bool disconnected)
    {
        using var context = new BunitContext();
        var runtime = new PendingImport();
        var module = new Module { Disconnected = disconnected };
        runtime.Completion.SetResult(module);
        context.Services.AddSingleton<IJSRuntime>(runtime);
        var cut = context.Render<InteropDemo>();
        cut.WaitForAssertion(() => Assert.False(cut.FindAll("button")[1].HasAttribute("disabled")));
        cut.FindAll("button")[1].Click();
        Assert.Same(cut.Instance, module.Reference!.Value);
        var reference = module.Reference;
        await cut.InvokeAsync(() => cut.Instance.DisposeAsync().AsTask());
        Assert.Equal(1, module.Disposals);
        Assert.Throws<ObjectDisposedException>(() => reference.Value);
        await cut.Instance.ReceiveTime("Late callback ignored");
    }

    public sealed class ImportObserver : InteropDemo
    {
        public Task? FirstAfterRender { get; private set; }
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            var actual = base.OnAfterRenderAsync(firstRender);
            if (firstRender) FirstAfterRender = actual;
            return actual;
        }
    }
    // BL0016 asks app code to guard JS interop calls with try/catch.
    // These two classes stand in for the JavaScript runtime in tests.
#pragma warning disable BL0016
    private sealed class PendingImport : IJSRuntime
    {
        public int Imports { get; private set; }
        public TaskCompletionSource<IJSObjectReference> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeCoreAsync<TValue>(identifier, default, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken token, object?[]? args) => InvokeCoreAsync<TValue>(identifier, token, args);
        private async ValueTask<TValue> InvokeCoreAsync<TValue>(string identifier, CancellationToken token, object?[]? args)
        {
            Assert.Equal("import", identifier); Imports++;
            return (TValue)(object)await Completion.Task;
        }
    }
    private sealed class Module : IJSObjectReference
    {
        public int Disposals { get; private set; }
        public int Invocations { get; private set; }
        public bool Disconnected { get; init; }
        public DotNetObjectReference<InteropDemo>? Reference { get; private set; }
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeCoreAsync<TValue>(identifier, default, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken token, object?[]? args) => InvokeCoreAsync<TValue>(identifier, token, args);
        private ValueTask<TValue> InvokeCoreAsync<TValue>(string identifier, CancellationToken token, object?[]? args)
        {
            Invocations++;
            if (identifier == "notifyTime") Reference = Assert.IsType<DotNetObjectReference<InteropDemo>>(args![0]);
            return ValueTask.FromResult(default(TValue)!);
        }
        public ValueTask DisposeAsync()
        {
            Disposals++;
            return Disconnected ? ValueTask.FromException(new JSDisconnectedException("Isolated disconnected circuit.")) : ValueTask.CompletedTask;
        }
    }
#pragma warning restore BL0016
}
