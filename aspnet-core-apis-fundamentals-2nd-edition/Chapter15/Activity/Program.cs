using Asp.Versioning;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Rewrite;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using WarehouseApi.Configuration;
using WarehouseApi.Endpoints;
using WarehouseApi.ErrorHandling;
using WarehouseApi.HealthChecks;
using WarehouseApi.Middleware;
using WarehouseApi.OpenApi;
using WarehouseApi.Persistence;
using WarehouseApi.RoutingConstraints;
using WarehouseApi.Services;
using WarehouseApi.Telemetry;

var builder = WebApplication.CreateBuilder(args);

// The DataContract XML formatters for clients that send Accept: application/xml to the controllers. Responses
// only: the request records have no data contracts, so an XML request body gets 415 instead of a 500
builder.Services.AddControllers()
    .AddXmlDataContractSerializerFormatters()
    .AddMvcOptions(options => options.InputFormatters.RemoveType<XmlDataContractSerializerInputFormatter>());

builder.Services.AddApiVersioning(options =>
{
    options.ReportApiVersions = true;
    options.ApiVersionReader = new UrlSegmentApiVersionReader();

    // RFC 9745: a Deprecation header with the date 1.0 was deprecated, and a Link with rel="deprecation"
    options.Policies.Deprecate(ApiVersionConstants.V1)
        .Effective(new DateTimeOffset(2026, 11, 10, 0, 0, 0, TimeSpan.Zero))
        .Link("https://docs.example.com/warehouse-api/deprecation")
            .Title("Warehouse API deprecation policy")
            .Type("text/html");

    // RFC 8594: a Sunset header with the date 1.0 stops working, and a Link with rel="sunset"
    options.Policies.Sunset(ApiVersionConstants.V1)
        .Effective(new DateTimeOffset(2027, 11, 10, 0, 0, 0, TimeSpan.Zero))
        .Link("https://docs.example.com/warehouse-api/migrate-to-v2")
            .Title("Migrating to Warehouse API v2")
            .Type("text/html");
})
.AddMvc()
.AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// One OpenAPI document per API version; keep this list in step with the versions the API declares
builder.Services.AddOpenApi(ApiVersionConstants.V1DocumentName, options => options.UseApiVersionDocument());
builder.Services.AddOpenApi(ApiVersionConstants.V2DocumentName, options => options.UseApiVersionDocument());

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

// Shipment requests to the ERP: a bounded queue, a background processor and a resilient typed client
builder.Services.AddFulfillment(builder.Configuration);

// Degraded or Unhealthy when too many shipment requests sit in the dead-letter queue
builder.Services.AddHealthChecks()
    .AddCheck<DeadLetterQueueHealthCheck>("dead-letter-queue");

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

// The Chapter 13 URLs have no version segment. Moving them would break every existing client, so until
// 1.0's sunset they are rewritten to /api/v1 before routing; /api/v1 and /api/v2 URLs pass unchanged
app.UseRewriter(new RewriteOptions()
    .AddRewrite(@"(?i)^api/(?!v[0-9]+(?:\.[0-9]+)?/)(.*)$", "api/v1/$1", skipRemainingRules: true));

// Route matching now runs inside the middleware registered above
app.UseRouting();

// CORS before authorization: it answers preflight requests before any policy runs and adds its headers
// to 401 and 403 responses; authorization needs the user that authentication establishes
app.UseCors("IntegrationClients");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Every Minimal API endpoint in this group serves both versions unless it maps to one of them
var api = app.NewVersionedApi("WarehouseApi")
    .MapGroup("/api/v{version:apiVersion}")
    .HasDeprecatedApiVersion(ApiVersionConstants.V1)
    .HasApiVersion(ApiVersionConstants.V2);

api.MapProductEndpoints();
api.MapWarehouseEndpoints();
api.MapStockEndpoints();
api.MapStockQueryEndpoints();
api.MapAdminEndpoints();

// Unversioned: load balancers probe the same URL whatever the API version
app.MapHealthEndpoints();
app.MapHealthChecks("/health/fulfillment");

if (app.Environment.IsDevelopment())
{
    // Serves /openapi/v1.json and /openapi/v2.json
    app.MapOpenApi();

    // Swagger UI only displays the documents; describe the versions after every endpoint is mapped
    app.UseSwaggerUI(options =>
    {
        foreach (var description in app.DescribeApiVersions())
        {
            options.SwaggerEndpoint(
                $"/openapi/{description.GroupName}.json",
                description.GroupName.ToUpperInvariant());
        }
    });
}

app.Run();
