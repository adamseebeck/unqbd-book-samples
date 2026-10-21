using LibraryDesk.Components;
using Microsoft.AspNetCore.DataProtection;
using LibraryDesk.Hubs;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
// Local teaching checkpoint: no writes to the user's Data Protection key ring.
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();
builder.Services.AddSignalR(options => options.MaximumReceiveMessageSize = 4096);
var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

app.MapHub<CatalogHub>("/hubs/catalog");
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(LibraryDesk.Client._Imports).Assembly);
app.Run();
