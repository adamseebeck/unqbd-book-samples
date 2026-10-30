using Microsoft.EntityFrameworkCore;

public static class Chapter14Lab
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            await RetailDatabase.ResetAsync(
                db, Seed.CatalogAsync);
            var customer = await db.Customers.SingleAsync();
            customer.ShippingAddress = new Address
            {
                Street = "1 Sample Street", PostalCode = "12345"
            };
            db.Payments.Add(new CardPayment
            {
                Amount = new Money(12.50m), LastFour = "4242"
            });
            var pen = await db.Products
                .SingleAsync(p => p.Sku == "PEN-003");
            pen.IsDeleted = true;
            await db.SaveChangesAsync();
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            var visible = await db.Products.CountAsync();
            var all = await db.Products.IgnoreQueryFilters()
                .CountAsync();
            Console.WriteLine($"Products: {visible} visible, " +
                $"{all} including deleted");

            var stamped = await db.Products.IgnoreQueryFilters()
                .Where(p => p.Sku == "PEN-003")
                .Select(p => EF.Property<DateTime>(
                    p, "LastModifiedUtc"))
                .SingleAsync();
            var set = stamped > DateTime.UnixEpoch;
            Console.WriteLine($"Audit stamp set: {set}");

            var customer = await db.Customers.SingleAsync();
            var (street, code) = (customer.ShippingAddress.Street,
                customer.ShippingAddress.PostalCode);
            Console.WriteLine($"Ships to: {street}, {code}");

            var payment = await db.Payments
                .OfType<CardPayment>().SingleAsync();
            Console.WriteLine(
                $"Card payment: {payment.Amount.Value:F2}, " +
                $"ends {payment.LastFour}");

            var minimum = 10m;
            var priced = await db.Products
                .FromSql($"""
                    SELECT * FROM Products
                    WHERE Price >= {minimum}
                    """)
                .CountAsync();
            Console.WriteLine($"Raw SQL rows: {priced}");
        }
    }
}
