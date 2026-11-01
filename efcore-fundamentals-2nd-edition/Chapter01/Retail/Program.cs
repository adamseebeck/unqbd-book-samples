using Microsoft.EntityFrameworkCore;

var options = new DbContextOptionsBuilder<InventoryDbContext>()
    .UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;" +
        "Database=EfCoreBook_Retail;Integrated Security=true;" +
        "TrustServerCertificate=true")
    .Options;
using var db = new InventoryDbContext(options);

var threshold = 5;
var query = db.Products
    .Where(p => p.StockQuantity < threshold)
    .OrderBy(p => p.Id);
Console.WriteLine(query.ToQueryString());
