using LibraryDesk.Client.Models;

namespace LibraryDesk.Client.Services;

public interface ICatalogQuery
{
    Task<BookItem[]> GetAsync(CancellationToken cancellationToken = default);
}
