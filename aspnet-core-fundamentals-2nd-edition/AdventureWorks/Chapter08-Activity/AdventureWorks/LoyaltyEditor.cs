using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace AdventureWorks;

public sealed record LoyaltyChange(string Number);

// Activity only, and Development only: changes a stored loyalty
// number the way a back-office tool would.
public static class LoyaltyEditor
{
    public static WebApplication MapLoyaltyEditor(
        this WebApplication app)
    {
        if (!app.Environment.IsDevelopment()) return app;
        app.MapPut("/dev/loyalty/{name}", ChangeAsync);
        return app;
    }

    static async Task<IResult> ChangeAsync(string name,
        LoyaltyChange change, UserManager<ApplicationUser> users)
    {
        var email = $"{name}@{DevelopmentSeed.Domain}";
        var user = await users.FindByNameAsync(email);
        if (user is null) return Results.NotFound();
        user.LoyaltyNumber = change.Number;
        var result = await users.UpdateAsync(user);
        return result.Succeeded
            ? Results.NoContent() : Results.Problem();
    }
}
