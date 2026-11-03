using AdventureWorks.Core;

namespace AdventureWorks;

// The asynchronous check behind AvailableSkuAttribute. It asks
// the store the catalog uses and passes cancellation on.
public sealed class CatalogAvailability(IProductService products)
{
    public async Task<bool> IsAvailableAsync(
        string sku, CancellationToken cancellationToken) =>
        await products.GetBySkuAsync(sku, cancellationToken)
            is not null;
}
