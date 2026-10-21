using System.Net;
using System.Security.Claims;
using LibraryDesk.Components;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/security";
        options.AccessDeniedPath = "/denied";
        options.Cookie.Name = "LibraryDesk.Lab";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Events.OnRedirectToLogin = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api")) context.Response.StatusCode = 401;
            else context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            if (context.Request.Path.StartsWithSegments("/api")) context.Response.StatusCode = 403;
            else context.Response.Redirect(context.RedirectUri);
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization(options =>
    options.AddPolicy("Staff", policy => policy.RequireRole("Librarian")));
builder.Services.AddAntiforgery();
// Teaching fixture only: no machine key-ring writes; cookies expire on restart.
// A real deployment must configure and validate durable protected key storage.
builder.Services.AddDataProtection().UseEphemeralDataProtectionProvider();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
    app.UseHttpsRedirection();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

// These passwordless accounts are a local test fixture, never an identity system.
if (app.Environment.IsDevelopment())
{
    app.MapPost("/lab/signin/{account}", async (string account, HttpContext context,
        IAntiforgery antiforgery) =>
    {
        if (context.Connection.RemoteIpAddress is not { } ip || !IPAddress.IsLoopback(ip))
            return Results.StatusCode(403);
        if (!new[] { "reader", "librarian" }.Contains(account)) return Results.BadRequest();
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest("Invalid form token."); }
        var claims = new List<Claim> { new(ClaimTypes.Name, account) };
        if (account == "librarian") claims.Add(new(ClaimTypes.Role, "Librarian"));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
        return Results.LocalRedirect("/security");
    });
    app.MapPost("/lab/signout", async (HttpContext context, IAntiforgery antiforgery) =>
    {
        try { await antiforgery.ValidateRequestAsync(context); }
        catch (AntiforgeryValidationException) { return Results.BadRequest("Invalid form token."); }
        await context.SignOutAsync();
        return Results.LocalRedirect("/security");
    });
}

app.MapGet("/api/staff/count", () => Results.Ok(new { Count = 3 }))
    .RequireAuthorization("Staff");
app.MapGet("/healthz", () => Results.Ok(new { Status = "ok" }));
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(LibraryDesk.Client._Imports).Assembly);
app.Run();
