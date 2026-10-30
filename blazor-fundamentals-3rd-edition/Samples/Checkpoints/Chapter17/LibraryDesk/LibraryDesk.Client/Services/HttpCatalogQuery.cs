using System.Net.Http.Json;
using LibraryDesk.Client.Models;

namespace LibraryDesk.Client.Services;

public sealed class HttpCatalogQuery(HttpClient http)
    : ICatalogQuery
{
    public async Task<BookItem[]> GetAsync(
        CancellationToken cancellationToken = default)
    {
        using var response =
            await http.GetAsync("api/books", cancellationToken);
        response.EnsureSuccessStatusCode();
        var books = await response.Content
            .ReadFromJsonAsync<BookItem[]>(cancellationToken);
        return books ?? throw new InvalidDataException(
            "The catalog response was null.");
    }
}
