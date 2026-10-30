using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
namespace AdventureWorks.DataSample;

// Local test double, like Chapter 15's TestAuthHandler: Development only, fixed server-side users,
// and no claim values taken from the request. A real application signs users in through Identity (Chapter 8).
public static class DevSignIn
{
    public static WebApplication MapDevelopmentSignIn(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;
        app.MapPost("/dev/sign-in/{user}", async (string user, HttpContext context) =>
        {
            // owner-1 creates orders; owner-2 checks that another owner cannot load them.
            if (user is not ("owner-1" or "owner-2")) return Results.NotFound();
            Claim[] claims = [new(ClaimTypes.NameIdentifier, user), new(ClaimTypes.Name, user)];
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
