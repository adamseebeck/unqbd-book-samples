using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

await using var connection = new SqliteConnection(
    "Data Source=:memory:;Foreign Keys=True");
await connection.OpenAsync();
var options = new DbContextOptionsBuilder<SqliteContext>()
    .UseSqlite(connection)
    .Options;

await using (var setup = new SqliteContext(options))
{
    await setup.Database.EnsureCreatedAsync();
    setup.Add(new SqliteProduct
    {
        Sku = "ONE", Stock = 10,
        Category = new SqliteCategory { Name = "Books" }
    });
    await setup.SaveChangesAsync();
}

await using (var invalid = new SqliteContext(options))
{
    invalid.Add(new SqliteProduct
    {
        Sku = "BAD", Stock = 1, CategoryId = int.MaxValue
    });
    var refused = false;
    try
    {
        await invalid.SaveChangesAsync();
    }
    catch (DbUpdateException e) when (e.InnerException
        is SqliteException { SqliteErrorCode: 19 })
    {
        refused = true;
    }
    Expect(refused,
        "Unknown category: the foreign key refused the save.");
}

await using (var writer = new SqliteContext(options))
{
    await using var transaction =
        await writer.Database.BeginTransactionAsync();
    var product = await writer.Products.SingleAsync();
    product.Stock = 2;
    await writer.SaveChangesAsync();
    await transaction.RollbackAsync();
}
await using (var read = new SqliteContext(options))
{
    var stock = (await read.Products.SingleAsync()).Stock;
    Expect(stock == 10,
        $"Rolled back stock 2: a fresh context reads {stock}.");
}

await using var a = new SqliteContext(options);
await using var b = new SqliteContext(options);
var first = await a.Products.SingleAsync();
var second = await b.Products.SingleAsync();
var before = first.Version;
first.Stock = 8;
a.SaveChanges();
second.Stock = 7;
Expect(first.Version != before,
    "First writer, SaveChanges: Version has a new value.");

var conflict = false;
try
{
    await b.SaveChangesAsync();
}
catch (DbUpdateConcurrencyException)
{
    conflict = true;
}
Expect(conflict,
    "Second writer, old Version: DbUpdateConcurrencyException.");

await using var fresh = new SqliteContext(options);
var saved = (await fresh.Products.SingleAsync()).Stock;
Expect(saved == 8, $"Stock after the conflict: {saved}, " +
    "the first writer's value.");
Console.WriteLine(
    "SQLite has no rowversion, so SaveChanges sets Version.");

static void Expect(bool happened, string line)
{
    if (!happened)
        throw new InvalidOperationException(
            $"Unexpected result: {line}");
    Console.WriteLine(line);
}
