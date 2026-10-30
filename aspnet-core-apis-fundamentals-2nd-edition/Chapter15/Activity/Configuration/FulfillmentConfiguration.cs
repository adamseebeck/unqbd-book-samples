using Microsoft.Extensions.Options;
using WarehouseApi.Fulfillment;
using WarehouseApi.Integration;

namespace WarehouseApi.Configuration;

public static class FulfillmentConfiguration
{
    public static IServiceCollection AddFulfillment(this IServiceCollection services, IConfiguration configuration)
    {
        // Missing or invalid ERP settings stop the application at startup, not at the first shipment
        services.AddOptions<ExternalShipmentServiceOptions>()
            .Bind(configuration.GetSection(ExternalShipmentServiceOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<FulfillmentOptions>()
            .Bind(configuration.GetSection(FulfillmentOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<FulfillmentQueueOptions>()
            .Bind(configuration.GetSection(FulfillmentQueueOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<DeadLetterOptions>()
            .Bind(configuration.GetSection(DeadLetterOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // One queue for the application: the orders controller writes to it, the processor reads from it
        services.AddSingleton<IFulfillmentJobQueue, ChannelFulfillmentJobQueue>();
        services.AddScoped<FulfillmentJobHandler>();
        services.AddHostedService<FulfillmentProcessorBackgroundService>();

        // Jobs that failed every attempt, and the service that retries them later
        services.AddScoped<IFulfillmentDeadLetterQueue, ShipmentDeadLetterQueue>();
        services.AddHostedService<DeadLetterRetryBackgroundService>();

        // A typed client; IHttpClientFactory creates and recycles the handlers behind it
        services.AddHttpClient<IExternalShipmentService, ExternalShipmentService>((serviceProvider, client) =>
            {
                var erp = serviceProvider.GetRequiredService<IOptions<ExternalShipmentServiceOptions>>().Value;
                client.BaseAddress = new Uri(erp.BaseUrl);
                client.DefaultRequestHeaders.Add("X-API-Key", erp.ApiKey);
            })
            // Retries, timeouts and a circuit breaker, set in appsettings.json; the handler also turns off
            // HttpClient.Timeout so its own limits decide. Retries include this POST: its Idempotency-Key
            // header makes a repeated request harmless
            .AddStandardResilienceHandler(configuration.GetSection("ExternalShipmentService:Resilience"));

        // The default is 30 seconds. One ERP call may take TotalRequestTimeout (30 seconds) by itself,
        // so give the job in progress that long plus time to record the result
        services.Configure<HostOptions>(host => host.ShutdownTimeout = TimeSpan.FromSeconds(45));

        return services;
    }
}
