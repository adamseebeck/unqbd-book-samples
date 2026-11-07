using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace AdventureWorks;

// Development only: signs in a seeded account by name with the
// real Identity cookie, so requests.http and curl.exe can act as
// a user. It reads no claims from the request. Outside
// Development it maps nothing.
public static class DevSignIn
{
    public static WebApplication MapDevelopmentSignIn(
        this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;
        var dev = app.MapGroup("/dev").WithTags("Development")
            .AllowAnonymous();
        dev.MapPost("/sign-in/{name}", SignInAs);
        dev.MapPost("/sign-out", SignOutCurrent);
        return app;
    }

    static async Task<IResult> SignInAs(string name,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn)
    {
        var email = $"{name}@{DevelopmentSeed.Domain}";
        var user = await users.FindByNameAsync(email);
        if (user is null) return Results.NotFound();
        await signIn.SignInAsync(user, isPersistent: false);
        return Results.Ok(new { user.UserName });
    }

    static async Task<IResult> SignOutCurrent(
        SignInManager<ApplicationUser> signIn)
    {
        await signIn.SignOutAsync();
        return Results.NoContent();
    }
}
