using AdventureWorks.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<AdventureWorksDbContext>(o => o.UseSqlite(
    builder.Configuration.GetConnectionString("Orders") ?? "Data Source=orders-local.db"));
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IInventoryService, LocalInventory>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddAuthentication("Application")
    .AddScheme<AuthenticationSchemeOptions, RejectAuthentication>("Application", _ => { });
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.MapGet("/api/orders/{id:guid}", async (Guid id, AdventureWorksDbContext db) =>
    await db.Orders.AsNoTracking().SingleOrDefaultAsync(o => o.Id == id) is { } order
        ? Results.Ok(order) : Results.NotFound());
app.MapPost("/api/orders", async (CreateOrderRequest request, OrderService service, CancellationToken ct) =>
{
    try
    {
        var order = await service.CreateAsync(request, ct);
        return Results.Created($"/api/orders/{order.Id}", order);
    }
    catch (ArgumentException) { return Results.BadRequest(); }
    catch (InsufficientInventoryException) { return Results.Conflict(); }
});
app.MapDelete("/api/orders/{id:guid}", async (Guid id, AdventureWorksDbContext db) =>
    await db.Orders.Where(o => o.Id == id).ExecuteDeleteAsync() == 1
        ? Results.NoContent() : Results.NotFound()).RequireAuthorization(p => p.RequireRole("Administrator"));
app.Run();
public partial class Program { }
