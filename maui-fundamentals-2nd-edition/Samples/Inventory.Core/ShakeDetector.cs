namespace MauiInventory.Core;
public sealed class ShakeDetector
{
    private TimeSpan? firstPeak;
    private TimeSpan? lastEvent;
    public bool Observe(double x, double y, double z, TimeSpan elapsed)
    {
        if (lastEvent is { } last && elapsed - last < TimeSpan.FromSeconds(2))
            return false;
        if (!InventoryMetrics.IsShake(x, y, z, 2.5))
            return false;
        if (firstPeak is not { } first || elapsed - first > TimeSpan.FromMilliseconds(500) || elapsed < first)
        {
            firstPeak = elapsed;
            return false;
        }

        firstPeak = null;
        lastEvent = elapsed;
        return true;
    }

    public void Reset()
    {
        firstPeak = null;
        lastEvent = null;
    }
}
