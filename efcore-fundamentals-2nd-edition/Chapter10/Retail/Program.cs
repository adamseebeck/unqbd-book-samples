using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var lab = builder.Configuration["lab"];
if (lab is not null)
    builder.Logging.AddFilter(
        "Microsoft.Hosting.Lifetime", LogLevel.Warning);
var connection = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Set ConnectionStrings:DefaultConnection.");
var retry = !builder.Environment.IsDevelopment();
CommandLog.On = builder.Configuration.GetValue<bool>("ShowSql");
builder.Services.AddDbContextPool<InventoryDbContext>(options =>
    options.UseSqlServer(connection, sql =>
        {
            if (retry) sql.EnableRetryOnFailure(3);
        })
        .LogTo(CommandLog.Filter, CommandLog.Write));
builder.Services.AddScoped<IInventoryReader, InventoryReader>();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadiness>("database");

await using var app = builder.Build();
app.MapGet("/health/live", () => Results.Ok());
app.MapHealthChecks("/health/ready");

if (lab is not null)
{
    await app.StartAsync();
    await Labs.RunAsync(app.Services, lab);
    await app.StopAsync();
    return;
}

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await RetailDatabase.ResetAsync(
        scope.GetContext(), Seed.CatalogAsync);
}
app.Run();
