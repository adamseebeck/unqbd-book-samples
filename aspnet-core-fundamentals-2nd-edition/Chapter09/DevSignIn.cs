using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
namespace AdventureWorks.AuthorizationSample;

// Local test double, like Chapter 15's TestAuthHandler: Development only, fixed server-side users,
// and no claim values taken from the request. A real application signs users in through Identity (Chapter 8).
public static class DevSignIn
{
    public static WebApplication MapDevelopmentSignIn(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;
        app.MapPost("/dev/sign-in/{user}", async (string user, HttpContext context, TimeProvider clock) =>
        {
            var today = DateOnly.FromDateTime(clock.GetUtcNow().UtcDateTime);
            Claim[]? claims = user switch
            {
                "finance-owner" => [Id("owner-1"), Name("Finance owner"), Department("Finance"), Born(new DateOnly(1990, 1, 1))],
                "owner" => [Id("owner-1"), Name("Owner without Finance"), Born(today.AddYears(-18))],
                "finance-nonowner" => [Id("owner-2"), Name("Finance non-owner"), Department("Finance"), Born(new DateOnly(1990, 1, 1))],
                "admin" => [Id("admin-1"), Name("Admin"), new Claim(ClaimTypes.Role, "Admin")],
                _ => null
            };
            if (claims is null) return Results.NotFound();
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
    private static Claim Id(string value) => new(ClaimTypes.NameIdentifier, value);
    private static Claim Name(string value) => new(ClaimTypes.Name, value);
    private static Claim Department(string value) => new("Department", value);
    private static Claim Born(DateOnly value) => new("DateOfBirth", value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
}
