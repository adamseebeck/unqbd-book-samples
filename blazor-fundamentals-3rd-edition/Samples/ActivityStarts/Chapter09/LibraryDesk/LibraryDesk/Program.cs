using LibraryDesk.Client.Services;
using LibraryDesk.Components;
using LibraryDesk.Services;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
// Keep Data Protection keys in memory for this local app.
builder.Services.AddDataProtection()
    .UseEphemeralDataProtectionProvider();
builder.Services.AddScoped<FavoritesState>();
builder.Services.AddSingleton<ICatalogQuery,
    MemoryCatalogQuery>();
builder.Services.AddAntiforgery();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found",
    createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(LibraryDesk.Client._Imports).Assembly);

app.Run();
