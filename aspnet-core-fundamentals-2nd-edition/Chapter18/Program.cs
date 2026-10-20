using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateSlimBuilder(args);
builder.Logging.AddJsonConsole();
builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(15));
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.TypeInfoResolverChain.Insert(0, DeploymentJsonContext.Default));
builder.Services.AddSingleton<CatalogStore>();
builder.Services.AddHealthChecks().AddCheck<CatalogHealthCheck>("catalog", tags: ["ready"]);
var app = builder.Build();
app.MapHealthChecks("/healthz/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/healthz/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});
app.MapGet("/api/products", async (CatalogStore catalog, CancellationToken ct) =>
    TypedResults.Ok(await catalog.LoadAsync(ct)));
app.Lifetime.ApplicationStopping.Register(() => app.Logger.LogInformation("Shutdown started"));
app.Lifetime.ApplicationStopped.Register(() => app.Logger.LogInformation("Shutdown completed"));
app.Run();
