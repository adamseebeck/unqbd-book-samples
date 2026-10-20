using System.Security.Claims;
using AdventureWorks.DataSample;
using AdventureWorks.ServicesSample;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
builder.Host.UseDefaultServiceProvider(options => { options.ValidateScopes = true; options.ValidateOnBuild = true; });
builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlite(
    builder.Configuration.GetConnectionString("Orders") ?? "Data Source=service-training.db"));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<ICatalogRepository, CatalogRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<IOrderService>(services => new LoggingOrderServiceDecorator(
    services.GetRequiredService<OrderService>(), services.GetRequiredService<ILogger<LoggingOrderServiceDecorator>>()));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
    Results.Ok(new { Token = antiforgery.GetAndStoreTokens(context).RequestToken })).RequireAuthorization();
app.MapPost("/api/orders", async (CreateOrderRequest request, IOrderService service, ClaimsPrincipal user,
    HttpContext context, IAntiforgery antiforgery, CancellationToken ct) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }
    var owner = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (owner is null) return Results.Unauthorized();
    var result = await service.CreateAsync(owner, request, ct);
    return result.Succeeded ? Results.Created($"/api/orders/{result.OrderId}", result) : Results.BadRequest(result);
}).RequireAuthorization();
app.MapGet("/api/orders/{id:guid}", async (Guid id, IOrderService service, ClaimsPrincipal user, CancellationToken ct) =>
{
    var owner = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (owner is null) return Results.Unauthorized();
    var order = await service.GetAsync(owner, id, ct);
    return order is null ? Results.NotFound() : Results.Ok(new { order.Id, order.OrderNumber, order.Status,
        Items = order.Items.Select(i => new { i.ProductId, i.Quantity, i.UnitPriceCents }) });
}).RequireAuthorization();
app.Run();
