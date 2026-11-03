using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;

namespace AdventureWorks;

public sealed class CreateQuoteRequest
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, 20)]
    public int Quantity { get; set; }
}

public sealed record QuoteDto(
    int Id, int ProductId, int Quantity, decimal Total);

public sealed class QuoteStore
{
    private readonly ConcurrentDictionary<int, QuoteDto> _quotes
        = new();
    private int _lastId;

    public QuoteDto Add(
        int productId, int quantity, decimal total)
    {
        var id = Interlocked.Increment(ref _lastId);
        var quote = new QuoteDto(id, productId, quantity, total);
        _quotes[id] = quote;
        return quote;
    }

    public QuoteDto? Find(int id) =>
        _quotes.GetValueOrDefault(id);
}
