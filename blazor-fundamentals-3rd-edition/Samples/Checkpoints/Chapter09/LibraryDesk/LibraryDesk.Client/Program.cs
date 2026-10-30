using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LibraryDesk.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddScoped<FavoritesState>();

await builder.Build().RunAsync();
