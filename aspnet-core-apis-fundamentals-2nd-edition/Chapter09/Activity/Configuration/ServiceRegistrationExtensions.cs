using WarehouseApi.Persistence;
using WarehouseApi.Services;

namespace WarehouseApi.Configuration;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddWarehouseServices(this IServiceCollection services)
    {
        // Shared state lives as long as the process
        services.AddSingleton<InMemoryWarehouseStore>();
        services.AddSingleton<IdempotencyContext>();
        services.AddSingleton<IHealthService, HealthService>();

        // Application services live for one request
        services.AddScoped<ICorrelationContext, CorrelationContext>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IOrderService, OrderService>();

        return services;
    }
}
