using Microsoft.Extensions.DependencyInjection;

public static class Labs
{
    public const string Latest = "3";

    public static Task RunAsync(
        IServiceProvider services, string? lab) =>
        (lab ?? Latest) switch
        {
            "1" => Chapter01Lab.RunAsync(services),
            "2" => Chapter02Lab.RunAsync(services),
            "3" => Chapter03Lab.RunAsync(services),
            _ => throw new ArgumentException(
                $"There is no lab {lab}.")
        };

    public static InventoryDbContext GetContext(
        this IServiceScope scope) => scope.ServiceProvider
            .GetRequiredService<InventoryDbContext>();
}
