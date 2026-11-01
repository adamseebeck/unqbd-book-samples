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
    .AddCheck<DatabaseReadiness>("database")
    .AddCheck<MissingDatabaseReadiness>("missing");
if (lab is null)
    builder.Services.AddHostedService<InventoryWorker>();

await using var app = builder.Build();
app.UseMiddleware<OperationMiddleware>();
app.MapGet("/health/live", () => Results.Ok());
app.MapHealthChecks("/health/ready");
app.MapGet("/operation", (HttpContext http) => Results.Ok(new
{
    OperationId = http.Items["OperationId"],
    Products = http.Items["Products"]
}));

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
