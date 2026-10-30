using WarehouseApi.Services;

namespace WarehouseApi.Configuration;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddWarehouseServices(this IServiceCollection services)
    {
        // The in-memory store is gone; AddPersistence registers the EF Core services
        services.AddSingleton<IHealthService, HealthService>();
        services.AddScoped<ICorrelationContext, CorrelationContext>();

        return services;
    }
}
