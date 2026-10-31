public static class Labs
{
    public const string Latest = "2";

    public static Task RunAsync(
        IServiceProvider services, string? lab) =>
        (lab ?? Latest) switch
        {
            "1" => Chapter01Lab.RunAsync(services),
            "2" => Chapter02Lab.RunAsync(services),
            _ => throw new ArgumentException(
                $"There is no lab {lab}.")
        };
}
