using Microsoft.EntityFrameworkCore;
using WarehouseApi.Contracts;
using WarehouseApi.Mappings;
using WarehouseApi.Models;
using WarehouseApi.Persistence;

namespace WarehouseApi.Services;

public class EfProductService : IProductService
{
    private readonly WarehouseDbContext _context;

    public EfProductService(WarehouseDbContext context)
    {
        _context = context;
    }

    public async Task<ProductResponse?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);

        return product?.ToResponse();
    }

    public async Task<PagedResult<ProductResponse>> SearchAsync(string? searchTerm, bool? isActive, PaginationParameters pagination, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            // Becomes LIKE in SQL, which follows the column collation (case-insensitive by default)
            query = query.Where(p => p.Name.Contains(searchTerm) || p.Sku.Contains(searchTerm));
        }

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var products = await query
            .OrderBy(p => p.Sku)
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductResponse>(
            products.Select(p => p.ToResponse()).ToList(), totalCount, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<ProductResponse?> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        if (await _context.Products.AnyAsync(p => p.Sku == request.Sku, cancellationToken))
        {
            return null; // Duplicate SKU
        }

        var product = new Product
        {
            Sku = request.Sku,
            Name = request.Name,
            Description = request.Description,
            UnitPrice = request.UnitPrice,
            IsActive = true
        };

        _context.Products.Add(product);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SqlServerErrors.IsDuplicateKey(ex))
        {
            return null; // Another request added the same SKU after the check above
        }

        return product.ToResponse();
    }

    public async Task<ProductResponse?> UpdateAsync(string sku, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Sku == sku, cancellationToken);
        if (product is null)
        {
            return null;
        }

        // A tracked entity: SaveChangesAsync sends an UPDATE for the changed columns only
        product.Name = request.Name;
        product.Description = request.Description;
        product.UnitPrice = request.UnitPrice;
        // The endpoint has already answered a PUT without isActive with 400
        product.IsActive = request.IsActive!.Value;

        await _context.SaveChangesAsync(cancellationToken);
        return product.ToResponse();
    }
}
