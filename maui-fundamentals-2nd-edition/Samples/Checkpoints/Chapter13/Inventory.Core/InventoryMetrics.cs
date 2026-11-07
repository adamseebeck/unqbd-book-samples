namespace MauiInventory.Core;
public static class InventoryMetrics
{
    public static long StockValueCents(IEnumerable<Product> products) => products.Aggregate(0L, (total, p) => checked(total + checked(p.UnitPriceCents * p.Quantity)));
    public static bool IsShake(double x, double y, double z, double threshold = 2.0) => Math.Sqrt(x * x + y * y + z * z) > threshold;
}
