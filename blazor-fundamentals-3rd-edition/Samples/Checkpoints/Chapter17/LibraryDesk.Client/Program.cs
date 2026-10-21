using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LibraryDesk.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress), Timeout = TimeSpan.FromSeconds(15) });
builder.Services.AddScoped<ICatalogQuery, HttpCatalogQuery>();
builder.Services.AddScoped<FavoritesState>();
await builder.Build().RunAsync();
