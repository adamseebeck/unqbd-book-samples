using Microsoft.EntityFrameworkCore;
using WarehouseApi.Services;

namespace WarehouseApi.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Fail at startup, not at the first query, when the connection string is missing
        var connectionString = configuration.GetConnectionString("WarehouseDb")
            ?? throw new InvalidOperationException(
                "The connection string 'WarehouseDb' is missing. Add it to appsettings.Development.json or set ConnectionStrings__WarehouseDb.");

        // AddDbContext registers WarehouseDbContext as a Scoped service; the execution strategy
        // retries operations that fail with transient SQL Server errors, such as deadlocks
        services.AddDbContext<WarehouseDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null)));

        services.AddScoped<IProductService, EfProductService>();
        services.AddScoped<IWarehouseService, EfWarehouseService>();
        services.AddScoped<IInventoryService, EfInventoryService>();
        services.AddScoped<IOrderService, EfOrderService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IStockReleaseService, StockReleaseService>();
        services.AddScoped<IStockQueryService, StockQueryService>();

        return services;
    }
}
