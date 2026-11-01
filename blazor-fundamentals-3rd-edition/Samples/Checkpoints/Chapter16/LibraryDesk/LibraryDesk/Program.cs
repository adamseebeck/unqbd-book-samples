using System.Net;
using System.Security.Claims;
using LibraryDesk.Client.Services;
using LibraryDesk.Components;
using LibraryDesk.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(
        CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/security";
        options.AccessDeniedPath = "/denied";
        options.Cookie.Name = "LibraryDesk.Lab";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
                context.Response.StatusCode = 401;
            else
                context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api"))
                context.Response.StatusCode = 403;
            else
                context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("Staff",
        policy => policy.RequireRole("Librarian")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found",
    createScopeForStatusCodePages: true);
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// Passwordless test accounts for local Development runs only.
if (app.Environment.IsDevelopment())
{
    app.MapPost("/lab/signin/{account}", async (string account,
        HttpContext context, IAntiforgery antiforgery) =>
    {
        var ip = context.Connection.RemoteIpAddress;
        if (ip is null || !IPAddress.IsLoopback(ip))
            return Results.StatusCode(403);
        if (account is not ("reader" or "librarian"))
            return Results.BadRequest();
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest("Invalid form token.");
        }
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, account)
        };
        if (account == "librarian")
            claims.Add(new(ClaimTypes.Role, "Librarian"));
        var scheme =
            CookieAuthenticationDefaults.AuthenticationScheme;
        var identity = new ClaimsIdentity(claims, scheme);
        var principal = new ClaimsPrincipal(identity);
        await context.SignInAsync(scheme, principal);
        return Results.LocalRedirect("/security");
    });
    app.MapPost("/lab/signout", async (HttpContext context,
        IAntiforgery antiforgery) =>
    {
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException)
        {
            return Results.BadRequest("Invalid form token.");
        }
        await context.SignOutAsync();
        return Results.LocalRedirect("/security");
    });
}

app.MapGet("/api/books", async (ICatalogQuery catalog,
    CancellationToken token) => await catalog.GetAsync(token));
app.MapGet("/api/staff/count",
        () => Results.Ok(new { Count = 3 }))
    .RequireAuthorization("Staff");
app.MapGet("/healthz", () => Results.Ok(new { Status = "ok" }));

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(LibraryDesk.Client._Imports).Assembly);

app.Run();
