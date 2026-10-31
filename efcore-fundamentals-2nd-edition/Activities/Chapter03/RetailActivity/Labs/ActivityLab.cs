using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class ActivityLab
{
    public static async Task RunAsync(IServiceProvider services)
    {
        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            await RetailDatabase.ResetAsync(db);
            var supplier = new Supplier
            {
                CompanyName = "Northwind Paper",
                ContactEmail = "orders@northwind.test",
                CreatedAt = DateTime.UtcNow
            };
            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync();
            var length = db.Model
                .FindEntityType(typeof(Supplier))!
                .FindProperty(nameof(Supplier.CompanyName))!
                .GetMaxLength();
            Console.WriteLine($"Saved supplier {supplier.Id}");
            Console.WriteLine(
                $"CompanyName max length: {length}");
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            db.Suppliers.Add(new Supplier
            {
                CompanyName = "Copy Paper",
                ContactEmail = "orders@northwind.test",
                CreatedAt = DateTime.UtcNow
            });
            try
            {
                await db.SaveChangesAsync();
                Console.WriteLine("Saved the duplicate.");
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is SqlException sql)
            {
                Console.WriteLine("Duplicate email refused: " +
                    $"error {sql.Number}");
            }
        }
    }
}
