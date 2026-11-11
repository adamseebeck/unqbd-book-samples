using MauiInventory.Core;
using Xunit;

namespace Inventory.Tests;
public sealed class Activity13RenewalWindowTests
{
    [Theory]
    [InlineData(59, 1)]
    [InlineData(60, 2)]
    public async Task Renewal_respects_two_minutes(int elapsedSeconds, int expectedCalls)
    {
        var clock = new ActivityClock();
        var calls = 0;
        var coordinator = new AccessTokenCoordinator(_ =>
        {
            calls++;
            return Task.FromResult(new AccessSession("local-exercise", clock.Now.AddMinutes(3)));
        }, clock);
        await coordinator.GetAsync();
        Assert.Equal(1, calls);
        clock.Now = clock.Now.AddSeconds(elapsedSeconds);
        await coordinator.GetAsync();
        Assert.Equal(expectedCalls, calls);
    }

    private sealed class ActivityClock : TimeProvider
    {
        public DateTimeOffset Now = new(2026, 10, 15, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
