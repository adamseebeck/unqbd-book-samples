using SQLite;

namespace MauiInventory.Core;
public sealed class SqliteProductRepository : IProductRepository, IAsyncDisposable
{
    private readonly SQLiteAsyncConnection database;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private bool disposed;
    public SqliteProductRepository(string databasePath)
    {
        if (!Path.IsPathFullyQualified(databasePath))
            throw new ArgumentException("Use an absolute app-data path.", nameof(databasePath));
        database = new SQLiteAsyncConnection(databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.FullMutex);
    }

    private async Task<T> ExecuteAsync<T>(Func<SQLiteAsyncConnection, Task<T>> operation, CancellationToken token)
    {
        await gate.WaitAsync(token).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            if (!initialized)
            {
                await database.RunInTransactionAsync(db =>
                {
                    var version = db.ExecuteScalar<int>("PRAGMA user_version");
                    if (version > 1)
                        throw new InvalidDataException("This database schema is newer than this sample supports.");
                    var columns = db.GetTableInfo("Product");
                    if (columns.Count == 0)
                        db.CreateTable<Product>();
                    else if (version == 0)
                    {
                        // The supported v0 fixture has the current fields except UpdatedUtc.
                        string[] required = ["Id", "Sku", "Name", "Quantity", "UnitPriceCents"];
                        if (required.Any(name => !columns.Any(c => c.Name == name)))
                            throw new InvalidDataException("The legacy database shape is unsupported.");
                        if (!columns.Any(c => c.Name == "UpdatedUtc"))
                            db.Execute("ALTER TABLE Product ADD COLUMN UpdatedUtc bigint NOT NULL DEFAULT 0");
                        if (db.ExecuteScalar<int>("SELECT COUNT(*) FROM Product WHERE Sku IS NULL OR Name IS NULL OR Quantity IS NULL OR UnitPriceCents IS NULL OR trim(Sku) = '' OR trim(Name) = '' OR length(Sku) > 40 OR length(Name) > 120 OR Quantity < 0 OR UnitPriceCents < 0") > 0)
                            throw new InvalidDataException("Correct invalid legacy rows before upgrading.");
                    }

                    // A duplicate SKU fails inside this same transaction: neither the new
                    // column nor user_version is committed when the upgrade fails.
                    db.Execute("CREATE UNIQUE INDEX IF NOT EXISTS UX_Product_Sku ON Product(Sku)");
                    db.Execute("PRAGMA user_version = 1");
                }).ConfigureAwait(false);
                initialized = true;
            }

            token.ThrowIfCancellationRequested();
            // sqlite-net operations have no CancellationToken overload. An operation already
            // executing may finish; never report a committed write as if it were rolled back.
            return await operation(database).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default) => await ExecuteAsync(async db => await db.Table<Product>().OrderBy(p => p.Name).ToListAsync().ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
    public Task<Product?> GetAsync(int id, CancellationToken cancellationToken = default) => ExecuteAsync(async db => (Product? )await db.FindAsync<Product>(id).ConfigureAwait(false), cancellationToken);
    public async Task SaveAsync(Product product, CancellationToken cancellationToken = default)
    {
        product.Validate();
        await ExecuteAsync(async db =>
        {
            product.UpdatedUtc = DateTime.UtcNow;
            var count = product.Id == 0 ? await db.InsertAsync(product).ConfigureAwait(false) : await db.UpdateAsync(product).ConfigureAwait(false);
            if (count == 0)
                throw new InvalidOperationException("The product no longer exists.");
            return count;
        }, cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default) => await ExecuteAsync(db => db.DeleteAsync<Product>(id), cancellationToken).ConfigureAwait(false) > 0;
    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (disposed)
                return;
            disposed = true;
            await database.CloseAsync().ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    // Keep the semaphore valid so a racing or later call gets ObjectDisposedException.
    }
}
