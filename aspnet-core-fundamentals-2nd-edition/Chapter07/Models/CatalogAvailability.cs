namespace Chapter07Sample.Models;

public sealed class CatalogAvailability
{
    public Task<bool> IsAvailableAsync(string sku, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(sku == "NOTEBOOK-101");
    }
}
