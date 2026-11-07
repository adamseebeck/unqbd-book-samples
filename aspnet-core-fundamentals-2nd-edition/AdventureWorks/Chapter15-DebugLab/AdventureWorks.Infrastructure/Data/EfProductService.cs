using AdventureWorks.Core;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks.Infrastructure.Data;

// The catalog in the database, behind Chapter 4's interface. The
// catalog, the v1 and v2 APIs and the pages do not change.
public sealed class EfProductService(AdventureWorksDbContext db)
    : IProductService
{
    public async Task<Product?> GetByIdAsync(int id) =>
        ToProduct(await db.Products.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id));

    public async Task<Product[]> GetByIdsAsync(int[] ids,
        CancellationToken cancellationToken = default) =>
        Map(await db.Products.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToArrayAsync(cancellationToken));

    public async Task<Product?> GetBySkuAsync(string sku,
        CancellationToken cancellationToken = default) =>
        ToProduct(await db.Products.AsNoTracking()
            .SingleOrDefaultAsync(p => p.Sku == sku,
                cancellationToken));

    public async Task<Product[]> SearchAsync(
        string? query, int page, int pageSize)
    {
        var rows = db.Products.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query))
            rows = rows.Where(p =>
                p.Name.ToLower().Contains(query.ToLower()));
        return Map(await rows.OrderBy(p => p.Id)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToArrayAsync());
    }

    public async Task<Product[]> ListByCategoryAsync(
        string category) =>
        Map(await db.Products.AsNoTracking()
            .Where(p => p.Category.ToLower() ==
                category.ToLower())
            .OrderBy(p => p.Id).ToArrayAsync());

    public async Task<Product> CreateAsync(
        CreateProductRequest request)
    {
        var row = new ProductEntity
        {
            Name = request.Name,
            Sku = request.Sku,
            PriceCents = Money.ToCents(request.Price),
            Category = request.Category
        };
        db.Products.Add(row);
        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
            when (SqliteErrors.Is(ex, SqliteErrors.Unique))
        {
            // The unique index is the final check, even when two
            // requests race.
            throw new DomainValidationException(
                $"SKU {request.Sku} is already in the catalog.");
        }
        return ToProduct(row)!;
    }

    public async Task<bool> UpdateAsync(
        int id, UpdateProductRequest request) =>
        await SetAsync(db.Products.Where(p => p.Id == id),
            request) == 1;

    // The version check is part of the UPDATE's WHERE clause, so
    // no other writer can slip in between the check and the save.
    public async Task<UpdateOutcome> UpdateAsync(int id,
        UpdateProductRequest request, int expectedVersion)
    {
        var updated = await SetAsync(db.Products.Where(p =>
            p.Id == id && p.Version == expectedVersion), request);
        if (updated == 1) return UpdateOutcome.Updated;
        return await db.Products.AnyAsync(p => p.Id == id)
            ? UpdateOutcome.Conflict : UpdateOutcome.NotFound;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            return await db.Products.Where(p => p.Id == id)
                .ExecuteDeleteAsync() == 1;
        }
        catch (Exception ex) when (
            SqliteErrors.Is(ex, SqliteErrors.RestrictedDelete))
        {
            throw new DomainValidationException(
                $"Product {id} is on orders, so it stays.");
        }
    }

    static Task<int> SetAsync(IQueryable<ProductEntity> rows,
        UpdateProductRequest request) =>
        rows.ExecuteUpdateAsync(set => set
            .SetProperty(p => p.Name, request.Name)
            .SetProperty(p => p.PriceCents,
                Money.ToCents(request.Price))
            .SetProperty(p => p.Version, p => p.Version + 1));

    static Product? ToProduct(ProductEntity? row) =>
        row is null ? null : new Product(row.Id, row.Name,
            row.Sku, Money.FromCents(row.PriceCents),
            row.Category, row.Version);

    static Product[] Map(ProductEntity[] rows) =>
        [.. rows.Select(row => ToProduct(row)!)];
}
