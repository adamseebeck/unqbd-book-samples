using MauiInventory.Core;
using Xunit;

namespace Inventory.Tests;
public sealed class Activity15CacheTests
{
    [Fact]
    public async Task Cache_evicts_and_isolates()
    {
        var cache = new ThumbnailCache(8);
        var calls = 0;
        Task<byte[]> Decode(ThumbnailKey key, CancellationToken token)
        {
            calls++;
            return Task.FromResult(new byte[] { 1, 2, 3, 4, 5 });
        }
        var a = new ThumbnailKey("public-a", 1, 1);
        var b = new ThumbnailKey("public-b", 1, 1);
        var returned = await cache.GetAsync(a, Decode);
        returned[0] = 99;
        Assert.Equal(1, (await cache.GetAsync(a, Decode))[0]);
        Assert.Equal(1, calls);
        await cache.GetAsync(b, Decode);
        Assert.Equal(2, calls);
        await cache.GetAsync(a, Decode);
        Assert.Equal(3, calls);
    }
}
