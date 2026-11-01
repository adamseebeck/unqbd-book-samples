using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);
var connection = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Set ConnectionStrings:DefaultConnection.");
builder.Services.AddDbContext<ComplexContext>(options =>
    options.UseSqlServer(connection));

using var host = builder.Build();
using var scope = host.Services.CreateScope();
var db = scope.ServiceProvider
    .GetRequiredService<ComplexContext>();
var target = db.Database.GetDbConnection();
var safe = target.DataSource.StartsWith(
        "(localdb)", StringComparison.OrdinalIgnoreCase)
    && target.Database.StartsWith("EfCoreBook_");
if (!safe)
    throw new InvalidOperationException(
        "Use only a LocalDB EfCoreBook_ database.");

await db.Database.EnsureDeletedAsync();
await db.Database.MigrateAsync();
var applied = await db.Database.GetAppliedMigrationsAsync();
foreach (var id in applied)
    Console.WriteLine($"Applied: {id}");

db.Customers.Add(new Customer
{
    Name = "Alex",
    ShippingAddress = new Address
    {
        Street = "1 Sample Street", PostalCode = "12345"
    }
});
await db.SaveChangesAsync();
db.ChangeTracker.Clear();

var customer = await db.Customers
    .SingleAsync(c => c.ShippingAddress.PostalCode == "12345");
var (street, code) = (customer.ShippingAddress.Street,
    customer.ShippingAddress.PostalCode);
Console.WriteLine($"Ships to: {street}, {code}");

customer.ShippingAddress = new Address
{
    Street = "1 Sample Street", PostalCode = "12345"
};
db.ChangeTracker.DetectChanges();
foreach (var entry in db.ChangeTracker.Entries())
{
    var name = entry.Metadata.DisplayName();
    Console.WriteLine($"Tracked: {name}, {entry.State}");
}
