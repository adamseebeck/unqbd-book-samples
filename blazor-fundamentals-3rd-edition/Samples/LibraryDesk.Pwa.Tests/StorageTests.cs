using Bunit;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Xunit;

public sealed class StorageTests
{
    [Fact]
    public async Task SaveDoesNotRaceInitialStorageRead()
    {
        using var context = new BunitContext();
        var storage = new ControlledStorage();
        context.Services.AddSingleton<IJSRuntime>(storage);
        var cut = context.Render<LibraryDesk.Pwa.Pages.Home>();
        cut.Find("input").Change("New title");
        var attempt = cut.Find("button").TriggerEventAsync("onclick", new MouseEventArgs());
        try
        {
            await cut.InvokeAsync(() => { });
            Assert.Empty(storage.Writes);
            Assert.True(cut.Find("button").HasAttribute("disabled"));
        }
        finally
        {
            storage.FailWrites(); storage.Read.TrySetResult("[\"Existing title\"]");
            await attempt.WaitAsync(TimeSpan.FromSeconds(5));
        }
        cut.WaitForAssertion(() => Assert.Equal("Existing title", cut.Find("li").TextContent));
    }

    [Fact]
    public async Task QueuedSaveDoesNotOverlapAndFailedWriteCanBeRetried()
    {
        using var context = new BunitContext();
        var storage = new ControlledStorage(); storage.Read.SetResult("[]");
        context.Services.AddSingleton<IJSRuntime>(storage);
        var cut = context.Render<LibraryDesk.Pwa.Pages.Home>();
        cut.WaitForAssertion(() => Assert.Contains("Loaded 0 local titles.", cut.Markup));
        cut.Find("input").Change("New title");
        var first = cut.Find("button").TriggerEventAsync("onclick", new MouseEventArgs());
        cut.WaitForAssertion(() => Assert.Single(storage.Writes));
        var second = cut.Find("button").TriggerEventAsync("onclick", new MouseEventArgs());
        try
        {
            await cut.InvokeAsync(() => { });
            Assert.Single(storage.Writes);
            Assert.True(cut.Find("button").HasAttribute("disabled"));
        }
        finally
        {
            storage.FailWrites();
            await Task.WhenAll(first, second).WaitAsync(TimeSpan.FromSeconds(5));
        }
        cut.WaitForAssertion(() => Assert.Contains("Storage failed. The list was not saved.", cut.Markup));
        Assert.Empty(cut.FindAll("li"));
        Assert.False(cut.Find("button").HasAttribute("disabled"));
        var retry = cut.Find("button").TriggerEventAsync("onclick", new MouseEventArgs());
        cut.WaitForAssertion(() => Assert.Equal(2, storage.Writes.Count));
        storage.Writes[^1].Completion.SetResult(true);
        await retry.WaitAsync(TimeSpan.FromSeconds(5));
        cut.WaitForAssertion(() => Assert.Equal("New title", cut.Find("li").TextContent));
        Assert.Equal(new[] { "New title" }, System.Text.Json.JsonSerializer.Deserialize<string[]>(storage.Writes[^1].Json));
    }

    private sealed class ControlledStorage : IJSRuntime
    {
        public TaskCompletionSource<string?> Read { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public List<(string Json, TaskCompletionSource<bool> Completion)> Writes { get; } = [];
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => InvokeCoreAsync<TValue>(identifier, default, args);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken token, object?[]? args) => InvokeCoreAsync<TValue>(identifier, token, args);
        private async ValueTask<TValue> InvokeCoreAsync<TValue>(string identifier, CancellationToken token, object?[]? args)
        {
            if (identifier == "localStorage.getItem") return (TValue)(object)(await Read.Task)!;
            if (identifier != "localStorage.setItem") throw new JSException("Unexpected isolated storage operation.");
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            Writes.Add(((string)args![1]!, completion));
            await completion.Task;
            return default!;
        }
        public void FailWrites()
        {
            foreach (var write in Writes) write.Completion.TrySetException(new JSException("Controlled storage write failure."));
        }
    }
}
