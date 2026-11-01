using Microsoft.Extensions.DependencyInjection;

public static class Labs
{
    public const string Latest = "5";

    public static Task RunAsync(
        IServiceProvider services, string? lab) =>
        (lab ?? Latest) switch
        {
            "1" => Chapter01Lab.RunAsync(services),
            "2" => Chapter02Lab.RunAsync(services),
            "3" => Chapter03Lab.RunAsync(services),
            "4" => Chapter04Lab.RunAsync(services),
            "5" => Chapter05Lab.RunAsync(services),
            "activity" => ActivityLab.RunAsync(services),
            _ => throw new ArgumentException(
                $"There is no lab {lab}.")
        };

    public static InventoryDbContext GetContext(
        this IServiceScope scope) => scope.ServiceProvider
            .GetRequiredService<InventoryDbContext>();
}
