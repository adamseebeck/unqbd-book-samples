using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WarehouseApi.Configuration;
using WarehouseApi.Endpoints;
using WarehouseApi.ErrorHandling;
using WarehouseApi.Middleware;
using WarehouseApi.Persistence;
using WarehouseApi.RoutingConstraints;
using WarehouseApi.Services;
using WarehouseApi.Telemetry;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Built-in Minimal API validation
builder.Services.AddValidation();

// IProblemDetailsService for every error response, each one with the request's correlation ID
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["correlationId"] =
            context.HttpContext.RequestServices.GetRequiredService<ICorrelationContext>().CorrelationId;
});

// Turns exceptions into Problem Details for UseExceptionHandler
builder.Services.AddSingleton<IExceptionToProblemDetailsMapper, ExceptionToProblemDetailsMapper>();
builder.Services.AddExceptionHandler<WarehouseExceptionHandler>();

// JWT bearer tokens, the authorization policies, and the CORS policy for browser clients
builder.Services.AddWarehouseAuthentication(builder.Configuration, builder.Environment);
builder.Services.AddWarehouseAuthorization();
builder.Services.AddWarehouseCors(builder.Configuration);

// Request-scoped helpers, then the DbContext and the EF Core service implementations
builder.Services.AddWarehouseServices();
builder.Services.AddPersistence(builder.Configuration);

// Traces: ASP.NET Core's request activities and the application's own source, written to the console
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("WarehouseApi"))
    .WithTracing(tracing => tracing
        .AddSource("Microsoft.AspNetCore")
        .AddSource(WarehouseTelemetry.SourceName)
        .AddConsoleExporter());

builder.Services.AddRouting(options =>
{
    options.ConstraintMap.Add("sku", typeof(SkuRouteConstraint));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // A scope of its own, so the seeding DbContext is disposed before the first request
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
    await DbInitializer.SeedAsync(context);
}

// The outermost middleware: exceptions from everything registered after it reach the handler
app.UseExceptionHandler();

// Gives error responses without a body, such as 401 and 403, a Problem Details body
app.UseStatusCodePages();

if (!app.Environment.IsDevelopment())
{
    // HSTS keeps browsers on HTTPS for this host. The redirect answers plain HTTP only where the app
    // knows its HTTPS port; behind a TLS-terminating proxy the platform enforces HTTPS
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseMiddleware<CorrelationIdMiddleware>();

// Route matching now runs inside the middleware registered above
app.UseRouting();

// CORS before authorization: it answers preflight requests before any policy runs and adds its headers
// to 401 and 403 responses; authorization needs the user that authentication establishes
app.UseCors("IntegrationClients");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    // Serves the OpenAPI document at /openapi/v1.json
    app.MapOpenApi();
}

app.MapControllers();
app.MapProductEndpoints();
app.MapWarehouseEndpoints();
app.MapStockEndpoints();
app.MapStockQueryEndpoints();
app.MapAuditEndpoints();
app.MapHealthEndpoints();

app.Run();
