using Chapter07Sample.Models;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddSingleton<CatalogAvailability>();
builder.Services.AddLocalization();
builder.Services.AddValidation();
builder.Services.Configure<FormOptions>(options =>
    options.MultipartBodyLengthLimit = 1024 * 1024);

var app = builder.Build();
app.UseRequestLocalization(options => options.SetDefaultCulture("en").AddSupportedCultures("en", "fr").AddSupportedUICultures("en", "fr"));
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapRazorPages();
app.MapPost("/api/orders/validate", (OrderApiInput input) =>
    TypedResults.Ok(new { input.Sku, input.Quantity }));
app.Run();
