using Microsoft.Extensions.DependencyInjection;

public static class Labs
{
    public const string Latest = "11";

    public static Task RunAsync(
        IServiceProvider services, string? lab) =>
        (lab ?? Latest) switch
        {
            "1" => Chapter01Lab.RunAsync(services),
            "2" => Chapter02Lab.RunAsync(services),
            "3" => Chapter03Lab.RunAsync(services),
            "4" => Chapter04Lab.RunAsync(services),
            "5" => Chapter05Lab.RunAsync(services),
            "6" => Chapter06Lab.RunAsync(services),
            "7" => Chapter07Lab.RunAsync(services),
            "8" => Chapter08Lab.RunAsync(services),
            "9" => Chapter09Lab.RunAsync(services),
            "10" => Chapter10Lab.RunAsync(services),
            "11" => Chapter11Lab.RunAsync(services),
            "activity" => Chapter11Activity.RunAsync(services),
            _ => throw new ArgumentException(
                $"There is no lab {lab}.")
        };

    public static InventoryDbContext GetContext(
        this IServiceScope scope) => scope.ServiceProvider
            .GetRequiredService<InventoryDbContext>();
}
