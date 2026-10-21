using SQLite;

namespace MauiInventory.Core;
public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> ListAsync(CancellationToken token = default);
    Task AddAsync(Category category, CancellationToken token = default);
}

public sealed class CategoryRepository : ICategoryRepository, IAsyncDisposable
{
    private readonly SQLiteAsyncConnection db;
    private readonly SemaphoreSlim gate = new(1, 1);
    private bool initialized;
    private bool disposed;
    public CategoryRepository(string path)
    {
        if (!Path.IsPathFullyQualified(path))
            throw new ArgumentException("Use an absolute path.", nameof(path));
        db = new SQLiteAsyncConnection(path);
    }

    private async Task EnsureAsync()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!initialized)
        {
            await db.CreateTableAsync<Category>();
            initialized = true;
        }
    }

    public async Task<IReadOnlyList<Category>> ListAsync(CancellationToken token = default)
    {
        await gate.WaitAsync(token);
        try
        {
            await EnsureAsync();
            token.ThrowIfCancellationRequested();
            return await db.Table<Category>().OrderBy(x => x.Name).ToListAsync();
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task AddAsync(Category category, CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(category.Name) || category.Name.Length > 120)
            throw new ArgumentException("Enter a category name of at most 120 characters.");
        await gate.WaitAsync(token);
        try
        {
            await EnsureAsync();
            token.ThrowIfCancellationRequested();
            await db.InsertAsync(category);
        }
        finally
        {
            gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await gate.WaitAsync();
        try
        {
            if (disposed)
                return;
            disposed = true;
            await db.CloseAsync();
        }
        finally
        {
            gate.Release();
        }
    }
}
