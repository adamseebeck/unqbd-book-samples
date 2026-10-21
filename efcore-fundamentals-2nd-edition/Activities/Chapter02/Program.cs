using Microsoft.EntityFrameworkCore;

public sealed class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int StockQuantity { get; set; }
}
public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
}



public static class Program
{
    public static async Task Main(string[] args)
    {
        if (args.Contains("--missing-startup"))
        {
            var missingBuilder = WebApplication.CreateBuilder(Array.Empty<string>());
            var missingConnection = missingBuilder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Set ConnectionStrings:DefaultConnection.");
            missingBuilder.Services.AddDbContext<InventoryDbContext>(o => o.UseSqlServer(missingConnection));
            await using var unexpectedHost = missingBuilder.Build();
            return;
        }
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Staging" });
        builder.WebHost.UseUrls("http://127.0.0.1:50722");
        builder.Configuration["ConnectionStrings:DefaultConnection"] =
            @"Server=(localdb)\MSSQLLocalDB;Database=EfCoreBook_Config;Integrated Security=true;TrustServerCertificate=true";
        var connection = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing setting");
        builder.Services.AddDbContext<InventoryDbContext>(o => o.UseSqlServer(connection,
            sql => { if (!builder.Environment.IsDevelopment()) sql.EnableRetryOnFailure(3); }));
        await using var app = builder.Build();
        app.MapGet("/configuration", (InventoryDbContext db) => db.Database.ProviderName);
        await app.StartAsync();
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
            if (!db.Database.CreateExecutionStrategy().RetriesOnFailure)
                throw new InvalidOperationException("Staging retry policy was not selected.");
            using var client = new HttpClient();
            var body = await client.GetStringAsync("http://127.0.0.1:50722/configuration");
            if (!body.Contains("Microsoft.EntityFrameworkCore.SqlServer")) throw new InvalidOperationException(body);
            var executable = Environment.ProcessPath ?? throw new InvalidOperationException("Missing process path.");
            var start = new System.Diagnostics.ProcessStartInfo(executable)
                { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
                start.ArgumentList.Add(typeof(Program).Assembly.Location);
            start.ArgumentList.Add("--missing-startup");
            foreach (var key in start.Environment.Keys.Where(k => k.StartsWith("ConnectionStrings", StringComparison.OrdinalIgnoreCase)).ToArray())
                start.Environment.Remove(key);
            start.Environment["ASPNETCORE_ENVIRONMENT"] = "MissingConfigurationCheck";
            start.Environment["DOTNET_ENVIRONMENT"] = "MissingConfigurationCheck";
            using var child = System.Diagnostics.Process.Start(start) ?? throw new InvalidOperationException("Child did not start.");
            var output = child.StandardOutput.ReadToEndAsync(); var error = child.StandardError.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            try { await child.WaitForExitAsync(timeout.Token); }
            catch (OperationCanceledException) { child.Kill(entireProcessTree: true); await child.WaitForExitAsync(); throw; }
            _ = await output;
            if (child.ExitCode == 0 || !(await error).Contains("Set ConnectionStrings:DefaultConnection."))
                throw new InvalidOperationException("Missing-setting startup did not fail in the separate process.");
            Console.WriteLine("PASS: staging retries, HTTP provider read and separate-process missing-setting startup failure");
        }
        finally { await app.StopAsync(); }
    }
}
