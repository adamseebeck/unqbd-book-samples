using System.Threading.Channels;

namespace MauiInventory.Core;
public readonly record struct ThumbnailKey(string PublicAssetId, int WidthPixels, int HeightPixels);
public sealed class ThumbnailCache(long budgetBytes)
{
    private readonly SemaphoreSlim gate = new(1, 1);
    private readonly Dictionary<ThumbnailKey, (byte[] Data, long Used)> cache = [];
    private long bytes, sequence;
    public async Task<byte[]> GetAsync(ThumbnailKey key, Func<ThumbnailKey, CancellationToken, Task<byte[]>> decode, CancellationToken token = default)
    {
        if (budgetBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(budgetBytes));
        if (string.IsNullOrWhiteSpace(key.PublicAssetId) || key.WidthPixels is < 1 or > 2048 || key.HeightPixels is < 1 or > 2048)
            throw new ArgumentException("Supply a public asset identity and bounded pixel dimensions.", nameof(key));
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            if (cache.TryGetValue(key, out var hit))
            {
                cache[key] = (hit.Data, ++sequence);
                return hit.Data.ToArray();
            }

            var image = await decode(key, token).ConfigureAwait(false);
            token.ThrowIfCancellationRequested();
            if (image.LongLength > budgetBytes)
                return image; // Oversized results are never retained.
            while (bytes + image.LongLength > budgetBytes && cache.Count > 0)
            {
                var oldest = cache.MinBy(pair => pair.Value.Used);
                bytes -= oldest.Value.Data.LongLength;
                cache.Remove(oldest.Key);
            }

            cache.Add(key, (image.ToArray(), ++sequence));
            bytes += image.LongLength;
            return image;
        }
        finally
        {
            gate.Release();
        }
    }
}

public enum OperationCategory
{
    CatalogRead,
    LocalDatabase,
    PageLoad
}

public readonly record struct SafeMetric(OperationCategory Operation, long ElapsedMilliseconds, bool Succeeded);
public sealed class LocalMetricBuffer
{
    private readonly Channel<SafeMetric> channel = Channel.CreateBounded<SafeMetric>(new BoundedChannelOptions(64) { FullMode = BoundedChannelFullMode.Wait, SingleReader = true, SingleWriter = false, AllowSynchronousContinuations = false });
    // TryWrite drops new metrics when full. No wait or telemetry network call runs on the UI path.
    public bool TryRecord(SafeMetric metric) => metric.ElapsedMilliseconds >= 0 && channel.Writer.TryWrite(metric);
    public void Complete() => channel.Writer.TryComplete();
    public IAsyncEnumerable<SafeMetric> ReadAllAsync(CancellationToken token = default) => channel.Reader.ReadAllAsync(token);
}
