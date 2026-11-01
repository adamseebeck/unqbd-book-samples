using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

public static class ActivityLab
{
    public static async Task RunAsync(IServiceProvider services)
    {
        int id;
        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            await RetailDatabase.ResetAsync(db);
            var supplier = NewSupplier("Northwind Paper");
            db.Suppliers.Add(supplier);
            await db.SaveChangesAsync();
            id = supplier.Id;
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            var supplier = await db.Suppliers.SingleAsync(
                s => s.Id == id);
            Console.WriteLine(
                $"Read back: {supplier.CompanyName}");
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            var edit = new SupplierNameEdit(
                id, "Northwind Paper and Ink");
            var renamed = await RenameAsync(db, edit);
            Console.WriteLine(
                $"Renamed: {renamed.CompanyName}, " +
                $"{renamed.ContactEmail}");
            try
            {
                await RenameAsync(
                    db, new SupplierNameEdit(id, " "));
            }
            catch (ArgumentException)
            {
                Console.WriteLine(
                    "Blank name refused before saving.");
            }
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            db.Suppliers.Remove(
                await db.Suppliers.SingleAsync(s => s.Id == id));
            await db.SaveChangesAsync();
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            var any = await db.Suppliers.AnyAsync();
            Console.WriteLine($"Any suppliers left: {any}");
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            db.Suppliers.Add(NewSupplier("Copy Paper"));
            await db.SaveChangesAsync();
        }

        using (var scope = services.CreateScope())
        {
            var db = scope.GetContext();
            db.Suppliers.Add(NewSupplier("Paper Again"));
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

    private static async Task<Supplier> RenameAsync(
        InventoryDbContext db, SupplierNameEdit edit)
    {
        if (string.IsNullOrWhiteSpace(edit.CompanyName))
            throw new ArgumentException(
                "Company name is required.", nameof(edit));
        var supplier = await db.Suppliers.SingleAsync(
            s => s.Id == edit.SupplierId);
        supplier.CompanyName = edit.CompanyName;
        await db.SaveChangesAsync();
        return supplier;
    }

    private static Supplier NewSupplier(string name) => new()
    {
        CompanyName = name,
        ContactEmail = "orders@northwind.test",
        CreatedAt = DateTime.UtcNow
    };
}

public sealed record SupplierNameEdit(
    int SupplierId, string CompanyName);
