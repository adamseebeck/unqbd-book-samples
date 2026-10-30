using WarehouseApi.Services;

namespace WarehouseApi.Configuration;

public static class ServiceRegistrationExtensions
{
    public static IServiceCollection AddWarehouseServices(this IServiceCollection services)
    {
        // AddPersistence registers the EF Core services; the health checks replaced IHealthService
        services.AddScoped<ICorrelationContext, CorrelationContext>();

        return services;
    }
}
