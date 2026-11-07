using System.Security.Claims;
using AdventureWorks.Infrastructure;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks;

public sealed record SeedAccount(string Name, string Loyalty,
    string Department, string DateOfBirth);

// Development only. MigrateAsync applies the migrations the
// database lacks; then each part adds only what is missing.
public static class DevelopmentSeed
{
    public const string Domain = "adventure-works.test";

    public static readonly SeedAccount[] Accounts =
    [
        // name, loyalty number, Department, date of birth
        new("casey", "AW-001", "", "1990-04-12"),     // customer
        new("jordan", "AW-002", "Finance", "1985-09-30"), // buyer
        new("morgan", "", "Finance", "1980-01-15"),   // staff
        new("riley", "AW-003", "", "2010-06-01"),     // under 21
        new("avery", "", "", "1975-11-20")            // admin
    ];

    public static async Task SeedDevelopmentDataAsync(
        this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var services = scope.ServiceProvider;
        var db = services
            .GetRequiredService<AdventureWorksDbContext>();
        await db.Database.MigrateAsync();
        await StoreSeed.AddCatalogAsync(db);
        var password = app.Configuration["Training:Password"];
        if (string.IsNullOrEmpty(password))
        {
            app.Logger.LogWarning(
                "Set Training:Password to create test accounts.");
            return;
        }
        var users = services
            .GetRequiredService<UserManager<ApplicationUser>>();
        foreach (var account in Accounts)
            await CreateAsync(users, account, password);
        await AddAdminAsync(services, users);
        await StoreSeed.AddOrdersAsync(db, users);
    }

    static async Task CreateAsync(
        UserManager<ApplicationUser> users,
        SeedAccount account, string password)
    {
        var email = $"{account.Name}@{Domain}";
        if (await users.FindByNameAsync(email) is not null)
            return;
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            LoyaltyNumber = account.Loyalty
        };
        Check(await users.CreateAsync(user, password));
        List<Claim> claims =
            [new("DateOfBirth", account.DateOfBirth)];
        if (account.Department.Length > 0)
            claims.Add(new("Department", account.Department));
        Check(await users.AddClaimsAsync(user, claims));
    }

    static async Task AddAdminAsync(IServiceProvider services,
        UserManager<ApplicationUser> users)
    {
        var roles = services
            .GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roles.RoleExistsAsync("Admin"))
            Check(await roles.CreateAsync(
                new IdentityRole("Admin")));
        var avery =
            await users.FindByNameAsync($"avery@{Domain}");
        if (avery is not null &&
            !await users.IsInRoleAsync(avery, "Admin"))
            Check(await users.AddToRoleAsync(avery, "Admin"));
    }

    static void Check(IdentityResult result)
    {
        if (!result.Succeeded)
            throw new InvalidOperationException(string.Join(
                "; ", result.Errors.Select(e => e.Description)));
    }
}
