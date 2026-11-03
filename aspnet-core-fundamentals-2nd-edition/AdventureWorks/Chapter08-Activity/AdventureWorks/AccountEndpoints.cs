using System.Security.Claims;
using AdventureWorks.Infrastructure;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AdventureWorks;

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string Token);

public static class AccountEndpoints
{
    public static IEndpointRouteBuilder MapAccountEndpoints(
        this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/auth/login", LogIn);
        app.MapPost("/api/auth/refresh", Refresh);
        // The signed-in user through the default (cookie) scheme.
        app.MapGet("/api/account/me", Me).RequireAuthorization();
        // Bearer only: a valid cookie alone gets 401 here.
        app.MapGet("/api/account/loyalty",
                (ClaimsPrincipal user) => Me(user))
            .RequireAuthorization(policy => policy
                .AddAuthenticationSchemes(
                    JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser());
        return app;
    }

    static async Task<IResult> LogIn(LoginRequest request,
        UserManager<ApplicationUser> users,
        SignInManager<ApplicationUser> signIn,
        AdventureWorksDbContext db, TokenIssuer issuer,
        TimeProvider clock)
    {
        if (request.Email is not { Length: > 0 and <= 256 } ||
            request.Password is not { Length: > 0 and <= 1024 })
            return Results.Unauthorized();
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null) return Results.Unauthorized();
        var check = await signIn.CheckPasswordSignInAsync(
            user, request.Password, lockoutOnFailure: true);
        if (!check.Succeeded) return Results.Unauthorized();
        var refresh = TokenIssuer.NewRefreshToken();
        db.RefreshTokens.Add(NewRow(refresh, user, clock));
        await db.SaveChangesAsync();
        return Results.Ok(new
        {
            AccessToken = issuer.Issue(user),
            RefreshToken = refresh
        });
    }

    static async Task<IResult> Refresh(RefreshRequest request,
        AdventureWorksDbContext db,
        UserManager<ApplicationUser> users, TokenIssuer issuer,
        TimeProvider clock, CancellationToken cancellationToken)
    {
        if (request.Token is not { Length: > 0 and <= 256 })
            return Results.Unauthorized();
        var hash = TokenIssuer.Hash(request.Token);
        var old = await db.RefreshTokens.SingleOrDefaultAsync(
            t => t.TokenHash == hash, cancellationToken);
        if (old is null || old.ConsumedByHash is not null ||
            old.Expires <= clock.GetUtcNow())
            return Results.Unauthorized();
        var user = await users.FindByIdAsync(old.UserId);
        if (user is null || await users.IsLockedOutAsync(user))
            return Results.Unauthorized();
        var raw = TokenIssuer.NewRefreshToken();
        old.ConsumedByHash = TokenIssuer.Hash(raw);
        db.RefreshTokens.Add(NewRow(raw, user, clock));
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException)
        {
            return Results.Unauthorized(); // a replay lost
        }
        return Results.Ok(new
        {
            AccessToken = issuer.Issue(user),
            RefreshToken = raw
        });
    }

    static RefreshToken NewRow(string raw, ApplicationUser user,
        TimeProvider clock) => new()
    {
        TokenHash = TokenIssuer.Hash(raw),
        UserId = user.Id,
        Expires = clock.GetUtcNow().AddDays(7)
    };

    static IResult Me(ClaimsPrincipal user) => Results.Ok(new
    {
        Name = user.Identity?.Name,
        Scheme = user.Identity?.AuthenticationType,
        LoyaltyNumbers = user.FindAll(
                LoyaltyClaimsTransformation.ClaimType)
            .Select(claim => claim.Value)
    });
}
