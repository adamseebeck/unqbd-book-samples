using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

// This desktop-only demonstration has no reference from Inventory.App.
var watch = args.Contains("--watch", StringComparer.Ordinal);
var providerArgs = args.Where(value => value != "--watch").ToArray();
var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings { Args = providerArgs, ContentRootPath = AppContext.BaseDirectory, EnvironmentName = Environments.Development });
builder.Configuration.AddUserSecrets<Program>(optional: true, reloadOnChange: true);
builder.Services.AddOptions<DisplayOptions>().Bind(builder.Configuration.GetSection("Display")).Validate(value => value.PageSize is >= 1 and <= 100, "PageSize must be 1 through 100.");
using var host = builder.Build();
var options = host.Services.GetRequiredService<IOptionsMonitor<DisplayOptions>>();
Console.WriteLine($"Initial page size: {options.CurrentValue.PageSize}.");
Console.WriteLine($"Harmless local development value is present: {builder.Configuration["Demo:HarmlessValue"] is not null}.");
using var subscription = options.OnChange(value => Console.WriteLine($"Reloaded page size: {value.PageSize}."));
if (watch)
{
    Console.WriteLine("Change the copied output appsettings.Development.json, then press Enter to exit.");
    Console.ReadLine();
}

public sealed class DisplayOptions
{
    public int PageSize { get; set; } = 20;
}
