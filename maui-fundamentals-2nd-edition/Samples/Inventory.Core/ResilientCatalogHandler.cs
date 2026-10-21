using System.Net;

namespace MauiInventory.Core;
public sealed class CircuitOpenException : HttpRequestException
{
    public CircuitOpenException() : base("The catalog circuit is temporarily open.")
    {
    }
}

// A deliberately small GET-only policy. Writes need an idempotency contract before retries.
public sealed class ResilientCatalogHandler(TimeProvider clock, Func<TimeSpan, CancellationToken, Task>? wait = null) : DelegatingHandler
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Func<TimeSpan, CancellationToken, Task> delay = wait ?? ((duration, token) => Task.Delay(duration, clock, token));
    private int failedOperations;
    private DateTimeOffset openUntil = DateTimeOffset.MinValue;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
    {
        if (request.Method != HttpMethod.Get || request.Content is not null)
            return await base.SendAsync(request, token).ConfigureAwait(false);
        // This teaching policy serializes reads, including the first trial after cooldown.
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (clock.GetUtcNow() < openUntil)
                throw new CircuitOpenException();
            for (int attempt = 0; attempt < 3; attempt++)
            {
                using var copy = new HttpRequestMessage(HttpMethod.Get, request.RequestUri)
                {
                    Version = request.Version,
                    VersionPolicy = request.VersionPolicy
                };
                foreach (var header in request.Headers)
                    copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
                HttpResponseMessage? response = null;
                try
                {
                    response = await base.SendAsync(copy, token).ConfigureAwait(false);
                    if (!Transient(response.StatusCode))
                    {
                        failedOperations = 0;
                        return response;
                    }

                    if (attempt == 2)
                    {
                        RecordFailure();
                        return response; // CatalogClient owns/disposes this final response.
                    }
                }
                catch (HttpRequestException)when (!token.IsCancellationRequested)
                {
                    if (attempt == 2)
                    {
                        RecordFailure();
                        throw;
                    }
                }

                response?.Dispose();
                await delay(TimeSpan.FromMilliseconds(100 * (1 << attempt)), token).ConfigureAwait(false);
            }

            throw new InvalidOperationException("Unreachable retry state.");
        }
        finally
        {
            gate.Release();
        }
    }

    private void RecordFailure()
    {
        if (++failedOperations >= 2)
            openUntil = clock.GetUtcNow().AddSeconds(30);
    }

    private static bool Transient(HttpStatusCode status) => status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)status >= 500;
}

public interface IConnectivityStatus
{
    bool CanAttempt { get; }

    event EventHandler? Changed;
}

public sealed class ConnectivitySyncCoordinator(IConnectivityStatus connectivity, SyncQueueProcessor processor)
{
    public async Task<bool> SyncAsync(CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (!connectivity.CanAttempt)
            return false;
        // An Internet indication permits an attempt; it does not prove endpoint reachability.
        await processor.ProcessAsync(token).ConfigureAwait(false);
        return true;
    }
}
