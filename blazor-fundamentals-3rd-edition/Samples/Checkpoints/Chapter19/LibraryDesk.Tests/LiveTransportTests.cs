using System.Net;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Bunit;
using LibraryDesk.Client.Pages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

public sealed class LiveTransportTests
{
    private readonly ITestOutputHelper output;
    public LiveTransportTests(ITestOutputHelper output) => this.output = output;

    [Fact]
    public async Task AbortedPendingAnnounceBecomesRecoverableSendFailure()
    {
        await using var fixture = await TransportFixture.StartAsync(abortPendingAnnounce: true);
        using var context = new BunitContext();
        context.Services.AddSingleton<NavigationManager>(new FixtureNavigation(fixture.Address));
        var cut = context.Render<LiveCatalog>();
        cut.WaitForAssertion(() => Assert.Contains("Connected.", cut.Markup), TimeSpan.FromSeconds(10));
        var connection = (HubConnection)typeof(LiveCatalog).GetField("connection", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cut.Instance)!;
        Exception? reconnecting = null;
        connection.Reconnecting += error => { reconnecting = error; return Task.CompletedTask; };
        cut.Find("input").Change("Abrupt transport proof");
        var sending = cut.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await fixture.InvocationReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var callbackError = await Record.ExceptionAsync(async () => await sending.WaitAsync(TimeSpan.FromSeconds(10)));
        output.WriteLine($"Actual reconnecting exception: {reconnecting}");
        output.WriteLine($"Actual send callback exception: {callbackError}");
        Assert.True(fixture.AbortedInvocations > 0);
        Assert.Null(callbackError);
        cut.WaitForAssertion(() =>
        {
            Assert.NotNull(reconnecting);
            Assert.True(cut.Markup.Contains("Send failed. Reconnect before retrying.") ||
                cut.Markup.Contains("Reconnecting...") || cut.Markup.Contains("Connected again; missed notices are not replayed."));
            Assert.DoesNotContain("Use a message from 1 to 120 characters.", cut.Markup);
        }, TimeSpan.FromSeconds(10));
        fixture.AbortPendingAnnounce = false;
        fixture.CompleteAnnounce = true;
        cut.WaitForAssertion(() => Assert.Equal(HubConnectionState.Connected, connection.State), TimeSpan.FromSeconds(10));
        cut.WaitForAssertion(() => Assert.False(cut.Find("button").HasAttribute("disabled")), TimeSpan.FromSeconds(10));
        cut.Find("input").Change("Recovered abrupt transport");
        await cut.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.WaitForAssertion(() => Assert.Contains("Recovered abrupt transport", cut.Find("li").TextContent));
    }

    [Fact]
    public async Task RefusedNegotiatedUpgradeBecomesRecoverableInitialFailure()
    {
        await using var fixture = await TransportFixture.StartAsync(refuseUpgrade: true);
        using var context = new BunitContext();
        context.Services.AddSingleton<NavigationManager>(new FixtureNavigation(fixture.Address));
        var cut = context.Render<LiveCatalog>();
        cut.WaitForAssertion(() => Assert.Contains("Initial connection failed. Reload to retry.", cut.Markup), TimeSpan.FromSeconds(10));
        Assert.True(fixture.RefusedUpgrades > 0);
        Assert.True(cut.Find("button").HasAttribute("disabled"));
        fixture.RefuseUpgrade = false;
        fixture.CompleteAnnounce = true;
        var reloaded = context.Render<LiveCatalog>();
        reloaded.WaitForAssertion(() => Assert.Contains("Connected.", reloaded.Markup), TimeSpan.FromSeconds(10));
        reloaded.Find("input").Change("Recovered refused upgrade");
        await reloaded.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        reloaded.WaitForAssertion(() => Assert.Contains("Recovered refused upgrade", reloaded.Find("li").TextContent));
    }

    [Fact]
    public async Task ClosedBeforeHandshakeBecomesRecoverableInitialFailure()
    {
        await using var fixture = await TransportFixture.StartAsync(closeBeforeHandshake: true);
        using var context = new BunitContext();
        context.Services.AddSingleton<NavigationManager>(new FixtureNavigation(fixture.Address));
        var cut = context.Render<LiveCatalog>();
        cut.WaitForAssertion(() => Assert.Contains("Initial connection failed. Reload to retry.", cut.Markup), TimeSpan.FromSeconds(10));
        Assert.True(fixture.HandshakeReceived.Task.IsCompletedSuccessfully);
        var failedConnection = (HubConnection)typeof(LiveCatalog).GetField("connection", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cut.Instance)!;
        Assert.Equal(HubConnectionState.Disconnected, failedConnection.State);
        output.WriteLine($"Actual failed initial connection state: {failedConnection.State}");
        Assert.True(cut.Find("button").HasAttribute("disabled"));
        fixture.CloseBeforeHandshake = false;
        fixture.CompleteAnnounce = true;
        var reloaded = context.Render<LiveCatalog>();
        reloaded.WaitForAssertion(() => Assert.Contains("Connected.", reloaded.Markup), TimeSpan.FromSeconds(10));
        reloaded.Find("input").Change("Recovered initial connection");
        await reloaded.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        reloaded.WaitForAssertion(() => Assert.Contains("Recovered initial connection", reloaded.Find("li").TextContent));
    }

    [Fact]
    public async Task PendingAnnounceTransportTimeoutBecomesRecoverableSendFailure()
    {
        await using var fixture = await TransportFixture.StartAsync();
        using var context = new BunitContext();
        context.Services.AddSingleton<NavigationManager>(new FixtureNavigation(fixture.Address));
        var cut = context.Render<LiveCatalog>();
        cut.WaitForAssertion(() => Assert.Contains("Connected.", cut.Markup), TimeSpan.FromSeconds(10));
        var connection = (HubConnection)typeof(LiveCatalog).GetField("connection", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cut.Instance)!;
        connection.ServerTimeout = TimeSpan.FromMilliseconds(250);
        Exception? reconnecting = null;
        connection.Reconnecting += error => { reconnecting = error; return Task.CompletedTask; };
        // Receiving this real protocol completion resets the actual client's
        // server-timeout deadline after changing the public timeout property.
        await connection.InvokeAsync("FixtureReady").WaitAsync(TimeSpan.FromSeconds(10));
        cut.Find("input").Change("Timeout proof");
        var sending = cut.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await fixture.InvocationReceived.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await sending.WaitAsync(TimeSpan.FromSeconds(10));
        cut.WaitForAssertion(() =>
        {
            Assert.IsType<TimeoutException>(reconnecting);
            Assert.True(cut.Markup.Contains("Send failed. Reconnect before retrying.") ||
                cut.Markup.Contains("Reconnecting...") || cut.Markup.Contains("Connected again; missed notices are not replayed."));
            Assert.DoesNotContain("Use a message from 1 to 120 characters.", cut.Markup);
        }, TimeSpan.FromSeconds(10));
        fixture.CompleteAnnounce = true;
        connection.ServerTimeout = TimeSpan.FromSeconds(30);
        cut.WaitForAssertion(() => Assert.Equal(HubConnectionState.Connected, connection.State), TimeSpan.FromSeconds(10));
        await connection.InvokeAsync("FixtureReady").WaitAsync(TimeSpan.FromSeconds(10));
        cut.WaitForAssertion(() => Assert.False(cut.Find("button").HasAttribute("disabled")), TimeSpan.FromSeconds(10));
        cut.Find("input").Change("Recovered pending send");
        await cut.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        cut.WaitForAssertion(() => Assert.Contains("Recovered pending send", cut.Find("li").TextContent));
    }

    [Fact]
    public async Task HubBusinessErrorKeepsItsValidationOutcome()
    {
        await using var fixture = await TransportFixture.StartAsync(rejectInvocation: true);
        using var context = new BunitContext();
        context.Services.AddSingleton<NavigationManager>(new FixtureNavigation(fixture.Address));
        var cut = context.Render<LiveCatalog>();
        cut.WaitForAssertion(() => Assert.Contains("Connected.", cut.Markup), TimeSpan.FromSeconds(10));
        cut.Find("input").Change("Rejected fixture message");
        await cut.Find("button").ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        Assert.Contains("Use a message from 1 to 120 characters.", cut.Markup);
        Assert.False(cut.Find("button").HasAttribute("disabled"));
        Assert.True(fixture.InvocationReceived.Task.IsCompletedSuccessfully);
    }

    private sealed class FixtureNavigation : NavigationManager
    {
        public FixtureNavigation(string address) => Initialize(address + "/", address + "/");
        protected override void NavigateToCore(string uri, bool forceLoad)
        {
            Uri = ToAbsoluteUri(uri).ToString();
            NotifyLocationChanged(false);
        }
    }

    private sealed class TransportFixture : IAsyncDisposable
    {
        private readonly WebApplication app;
        public string Address { get; private set; } = "";
        public TaskCompletionSource HandshakeReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource InvocationReceived { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public volatile bool CloseBeforeHandshake;
        public volatile bool CompleteAnnounce;
        public volatile bool RefuseUpgrade;
        public volatile bool AbortPendingAnnounce;
        private int refusedUpgrades;
        private int abortedInvocations;
        public int RefusedUpgrades => Volatile.Read(ref refusedUpgrades);
        public int AbortedInvocations => Volatile.Read(ref abortedInvocations);
        private TransportFixture(WebApplication app) => this.app = app;
        public static async Task<TransportFixture> StartAsync(bool closeBeforeHandshake = false, bool rejectInvocation = false, bool refuseUpgrade = false, bool abortPendingAnnounce = false)
        {
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseKestrel().UseUrls("http://127.0.0.1:0");
            var app = builder.Build();
            var fixture = new TransportFixture(app);
            fixture.CloseBeforeHandshake = closeBeforeHandshake;
            fixture.RefuseUpgrade = refuseUpgrade;
            fixture.AbortPendingAnnounce = abortPendingAnnounce;
            app.UseWebSockets();
            app.MapPost("/hubs/catalog/negotiate", () => Results.Json(new
            {
                negotiateVersion = 1,
                connectionId = "local-fixture",
                connectionToken = "local-fixture",
                availableTransports = new[] { new { transport = "WebSockets", transferFormats = new[] { "Text" } } }
            }));
            app.Map("/hubs/catalog", async context =>
            {
                if (!context.WebSockets.IsWebSocketRequest) { context.Response.StatusCode = 400; return; }
                if (fixture.RefuseUpgrade)
                {
                    Interlocked.Increment(ref fixture.refusedUpgrades);
                    context.Response.StatusCode = 503;
                    return;
                }
                using var socket = await context.WebSockets.AcceptWebSocketAsync();
                try
                {
                    var handshake = await ReceiveRecordAsync(socket, context.RequestAborted);
                    Assert.Contains("\"protocol\":\"json\"", handshake);
                    fixture.HandshakeReceived.TrySetResult();
                    if (fixture.CloseBeforeHandshake)
                    {
                        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "fixture closes before handshake response", context.RequestAborted);
                        return;
                    }
                    await SendAsync(socket, "{}\u001e", context.RequestAborted);
                    while (!context.RequestAborted.IsCancellationRequested && socket.State == WebSocketState.Open)
                    {
                        var frame = await ReceiveRecordAsync(socket, context.RequestAborted);
                        if (frame.Length == 0) break;
                        using var document = JsonDocument.Parse(frame);
                        var message = document.RootElement;
                        if (message.GetProperty("type").GetInt32() != 1) continue;
                        var target = message.GetProperty("target").GetString();
                        if (target == "FixtureReady")
                        {
                            await SendAsync(socket, JsonSerializer.Serialize(new { type = 3, invocationId = message.GetProperty("invocationId").GetString() }) + "\u001e", context.RequestAborted);
                            continue;
                        }
                        Assert.Equal("Announce", target);
                        fixture.InvocationReceived.TrySetResult();
                        if (fixture.AbortPendingAnnounce)
                        {
                            Interlocked.Increment(ref fixture.abortedInvocations);
                            socket.Abort();
                            return;
                        }
                        if (rejectInvocation)
                        {
                            var response = JsonSerializer.Serialize(new { type = 3, invocationId = message.GetProperty("invocationId").GetString(), error = "Fixture rejects the message" });
                            await SendAsync(socket, response + "\u001e", context.RequestAborted);
                        }
                        else if (fixture.CompleteAnnounce)
                        {
                            var notice = message.GetProperty("arguments")[0].GetString();
                            var notification = JsonSerializer.Serialize(new { type = 1, target = "CatalogNotice", arguments = new[] { notice } });
                            var completion = JsonSerializer.Serialize(new { type = 3, invocationId = message.GetProperty("invocationId").GetString() });
                            await SendAsync(socket, notification + "\u001e" + completion + "\u001e", context.RequestAborted);
                        }
                        // Timeout fixture deliberately sends neither a completion nor server pings.
                    }
                }
                catch (OperationCanceledException) { }
                catch (WebSocketException) { }
            });
            await app.StartAsync();
            fixture.Address = app.Urls.Single();
            return fixture;
        }
        private static async Task<string> ReceiveRecordAsync(WebSocket socket, CancellationToken token)
        {
            var bytes = new byte[4096];
            using var stream = new MemoryStream();
            while (true)
            {
                var read = await socket.ReceiveAsync(bytes.AsMemory(), token);
                if (read.MessageType == WebSocketMessageType.Close) return "";
                stream.Write(bytes, 0, read.Count);
                if (read.EndOfMessage) break;
            }
            return Encoding.UTF8.GetString(stream.ToArray()).TrimEnd('\u001e');
        }
        private static async Task SendAsync(WebSocket socket, string value, CancellationToken token) =>
            await socket.SendAsync(Encoding.UTF8.GetBytes(value).AsMemory(), WebSocketMessageType.Text, true, token);
        public async ValueTask DisposeAsync()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await app.StopAsync(timeout.Token);
            await app.DisposeAsync();
        }
    }
}
