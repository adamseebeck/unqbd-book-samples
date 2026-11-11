using MauiInventory.Core;
using System.Net;
using Xunit;

namespace Inventory.Tests;
public sealed class HttpPolicyTests
{
    private static readonly Uri Endpoint = new("https://inventory.example/catalog");
    [Fact]
    public async Task Unknown_length_stream_is_bounded_and_disposed_after_rejection()
    {
        var body = new GeneratedStream(1_048_577);
        var content = new StreamContent(body);
        Assert.Null(content.Headers.ContentLength);
        using var client = new HttpClient(new SingleResponse(content));
        await Assert.ThrowsAsync<InvalidDataException>(() => new CatalogClient(client, Endpoint).GetAsync());
        Assert.True(body.WasDisposed);
        Assert.InRange(body.ReadBytes, 1_048_577, 1_056_768);
    }

    [Fact]
    public async Task Caller_cancellation_interrupts_stream_read_and_disposes_response_body()
    {
        var body = new WaitingStream();
        using var client = new HttpClient(new SingleResponse(new StreamContent(body)));
        using var departure = new CancellationTokenSource();
        var operation = new CatalogClient(client, Endpoint).GetAsync(departure.Token);
        await body.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        departure.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => operation);
        Assert.True(body.ObservedToken.IsCancellationRequested);
        Assert.True(body.WasDisposed);
    }

    [Fact]
    public async Task Eligible_get_retries_dispose_failed_responses_and_keep_request_headers()
    {
        var inner = new ScriptedHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        var delays = new List<TimeSpan>();
        using var client = new HttpClient(new ResilientCatalogHandler(new Clock(), (delay, token) =>
        {
            delays.Add(delay);
            return Task.CompletedTask;
        }) { InnerHandler = inner });
        client.DefaultRequestHeaders.Add("X-Learning-Trace", "public-demo");
        using var response = await client.GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, inner.Calls);
        Assert.True(inner.Contents[0].Disposed);
        Assert.All(inner.Headers, value => Assert.Equal("public-demo", value));
        Assert.Equal(TimeSpan.FromMilliseconds(100), Assert.Single(delays));
    }

    [Theory]
    [InlineData(401)]
    [InlineData(404)]
    public async Task Authentication_and_not_found_failures_are_not_retried(int status)
    {
        var inner = new ScriptedHandler((HttpStatusCode)status);
        using var client = new HttpClient(new ResilientCatalogHandler(new Clock()) { InnerHandler = inner });
        using var response = await client.GetAsync(Endpoint);
        Assert.Equal(1, inner.Calls);
    }

    [Fact]
    public async Task Writes_are_sent_once_and_circuit_blocks_then_allows_one_serial_trial()
    {
        var inner = new ScriptedHandler(HttpStatusCode.ServiceUnavailable);
        var clock = new Clock();
        using var client = new HttpClient(new ResilientCatalogHandler(clock, (_, token) => Task.CompletedTask) { InnerHandler = inner });
        using (var post = await client.PostAsync(Endpoint, new StringContent("fictional")))
        {
            Assert.Equal(1, inner.Calls);
        }

        using (var first = await client.GetAsync(Endpoint))
        {
            Assert.Equal(4, inner.Calls);
        }

        using (var second = await client.GetAsync(Endpoint))
        {
            Assert.Equal(7, inner.Calls);
        }

        await Assert.ThrowsAsync<CircuitOpenException>(() => client.GetAsync(Endpoint));
        Assert.Equal(7, inner.Calls);
        clock.Now = clock.Now.AddSeconds(30);
        inner.Statuses = [HttpStatusCode.OK];
        using var trial = await client.GetAsync(Endpoint);
        Assert.Equal(HttpStatusCode.OK, trial.StatusCode);
        Assert.Equal(8, inner.Calls);
    }

    [Fact]
    public async Task Canceled_backoff_ends_without_an_extra_attempt()
    {
        var inner = new ScriptedHandler(HttpStatusCode.ServiceUnavailable);
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var client = new HttpClient(new ResilientCatalogHandler(new Clock(), async (_, token) =>
        {
            started.TrySetResult(true);
            await Task.Delay(Timeout.Infinite, token);
        }) { InnerHandler = inner });
        using var cancellation = new CancellationTokenSource();
        var request = client.GetAsync(Endpoint, cancellation.Token);
        await started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => request);
        Assert.Equal(1, inner.Calls);
        Assert.True(inner.Contents[0].Disposed);
    }

    [Fact]
    public async Task Offline_gate_skips_transport_but_reconnect_only_enables_an_explicit_sync()
    {
        var connectivity = new Connectivity();
        var transport = new Transport();
        var queue = new Queue();
        var coordinator = new ConnectivitySyncCoordinator(connectivity, new SyncQueueProcessor(queue, transport, new Clock()));
        Assert.False(await coordinator.SyncAsync());
        Assert.Equal(0, transport.Calls);
        connectivity.Set(true);
        Assert.Equal(0, transport.Calls);
        Assert.True(await coordinator.SyncAsync());
        Assert.Equal(1, transport.Calls);
    }

    private sealed class SingleResponse(HttpContent content) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content });
    }

    private sealed class TrackedContent : StringContent
    {
        public bool Disposed;
        public TrackedContent() : base("[]")
        {
        }

        protected override void Dispose(bool disposing)
        {
            Disposed = true;
            base.Dispose(disposing);
        }
    }

    private sealed class ScriptedHandler(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        public HttpStatusCode[] Statuses = statuses;
        public int Calls;
        public List<TrackedContent> Contents = [];
        public List<string> Headers = [];
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            var status = Statuses[Math.Min(Calls, Statuses.Length - 1)];
            Calls++;
            var content = new TrackedContent();
            Contents.Add(content);
            Headers.Add(request.Headers.TryGetValues("X-Learning-Trace", out var values) ? values.Single() : "");
            return Task.FromResult(new HttpResponseMessage(status) { Content = content });
        }
    }

    private class GeneratedStream(long length) : Stream
    {
        public bool WasDisposed;
        public long ReadBytes;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => ReadBytes; set => throw new NotSupportedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int size = (int)Math.Min(count, length - ReadBytes);
            Array.Fill(buffer, (byte)' ', offset, size);
            ReadBytes += size;
            return size;
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            int size = (int)Math.Min(buffer.Length, length - ReadBytes);
            buffer.Span[..size].Fill((byte)' ');
            ReadBytes += size;
            return ValueTask.FromResult(size);
        }

        protected override void Dispose(bool disposing)
        {
            WasDisposed = true;
            base.Dispose(disposing);
        }

        public override void Flush() => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

    private sealed class WaitingStream : GeneratedStream
    {
        public WaitingStream() : base(0)
        {
        }

        public TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken ObservedToken { get; private set; }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default)
        {
            ObservedToken = token;
            Started.TrySetResult(true);
            await Task.Delay(Timeout.Infinite, token);
            return 0;
        }
    }

    private sealed class Clock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 15, 0, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }

    private sealed class Connectivity : IConnectivityStatus
    {
        public bool CanAttempt { get; private set; }

        public event EventHandler? Changed;
        public void Set(bool value)
        {
            CanAttempt = value;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private sealed class Transport : ISyncTransport
    {
        public int Calls;
        public Task SendAsync(PendingSyncItem item, CancellationToken token)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class Queue : ISyncQueueStore
    {
        public Task<IReadOnlyList<PendingSyncItem>> ReadyAsync(DateTimeOffset now, CancellationToken token) => Task.FromResult<IReadOnlyList<PendingSyncItem>>([new() { ProductId = 1, Revision = 1 }]);
        public Task AcknowledgeAsync(int id, long revision, CancellationToken token) => Task.CompletedTask;
        public Task RecordFailureAsync(int id, long revision, DateTimeOffset now, DateTimeOffset next, CancellationToken token) => Task.CompletedTask;
    }
}
