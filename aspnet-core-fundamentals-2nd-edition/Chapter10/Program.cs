using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using AdventureWorks.DataSample;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    if (builder.Configuration["Database:Provider"] == "SqlServer")
        options.UseSqlServer(builder.Configuration.GetConnectionString("Orders")
            ?? throw new InvalidOperationException("Set ConnectionStrings__Orders for a disposable SQL Server database."));
    else options.UseSqlite(builder.Configuration.GetConnectionString("Orders") ?? "Data Source=orders-training.db");
});
builder.Services.AddScoped<OrderQueries>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie();
builder.Services.AddAuthorization();
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
var app = builder.Build();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapGet("/csrf", (HttpContext context, IAntiforgery antiforgery) =>
    Results.Ok(new { Token = antiforgery.GetAndStoreTokens(context).RequestToken })).RequireAuthorization();
app.MapGet("/api/orders/report", async (ClaimsPrincipal user, OrderQueries queries, int? page, CancellationToken ct) =>
{
    var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (id is null) return Results.Unauthorized();
    if (page is < 1 or > 10000) return Results.BadRequest();
    return Results.Ok(await queries.ReportAsync(id, page ?? 1, ct));
}).RequireAuthorization();
app.MapPut("/api/orders/{id:guid}", async (Guid id, UpdateOrderRequest request, ClaimsPrincipal user,
    ApplicationDbContext db, HttpContext context, IAntiforgery antiforgery, CancellationToken ct) =>
{
    try { await antiforgery.ValidateRequestAsync(context); }
    catch (AntiforgeryValidationException) { return Results.BadRequest(); }
    if (request.Notes is null || request.Notes.Length > 1000) return Results.BadRequest();
    var owner = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var order = await db.Orders.SingleOrDefaultAsync(o => o.Id == id && o.OwnerId == owner, ct);
    if (order is null) return Results.NotFound();
    db.Entry(order).Property(o => o.Version).OriginalValue = request.Version;
    order.Notes = request.Notes;
    try { await db.SaveChangesAsync(ct); return Results.Ok(new { order.Id, order.Version }); }
    catch (DbUpdateConcurrencyException) { return Results.Conflict(new { Error = "Reload the order before retrying." }); }
}).RequireAuthorization();
app.Run();
