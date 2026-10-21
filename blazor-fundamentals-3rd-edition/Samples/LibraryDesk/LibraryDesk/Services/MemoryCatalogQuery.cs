using LibraryDesk.Client.Models;
using LibraryDesk.Client.Services;

namespace LibraryDesk.Services;

public sealed class MemoryCatalogQuery : ICatalogQuery
{
    public Task<BookItem[]> GetAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new[]
        {
            new BookItem(1, "C# Field Notes", 24m),
            new BookItem(2, "Blazor Field Notes", 28m),
            new BookItem(3, "Database Field Notes", 26m)
        });
    }
}
