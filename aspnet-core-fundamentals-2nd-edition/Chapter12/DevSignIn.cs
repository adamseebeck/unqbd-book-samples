using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
namespace AdventureWorks.CacheSample;

// Local test double, like Chapter 15's TestAuthHandler: Development only, fixed server-side users,
// and no claim values taken from the request. A real application signs users in through Identity (Chapter 8).
public static class DevSignIn
{
    public static WebApplication MapDevelopmentSignIn(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;
        app.MapPost("/dev/sign-in/{user}", async (string user, HttpContext context) =>
        {
            // One Admin user for the protected cache-invalidation webhook.
            if (user != "admin") return Results.NotFound();
            Claim[] claims = [new(ClaimTypes.NameIdentifier, "admin-1"), new(ClaimTypes.Name, "Admin"), new(ClaimTypes.Role, "Admin")];
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
            return Results.Ok(new { SignedIn = user });
        }).AllowAnonymous();
        app.MapPost("/dev/sign-out", async (HttpContext context) =>
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }).AllowAnonymous();
        return app;
    }
}
