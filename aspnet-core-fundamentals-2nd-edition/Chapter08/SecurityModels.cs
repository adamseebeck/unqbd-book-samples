using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AdventureWorks.IdentitySample;

public sealed class ApplicationUser : IdentityUser
{
    public string LibraryCardNumber { get; set; } = "";
}

public sealed class RefreshToken
{
    public int Id { get; set; }
    public string TokenHash { get; set; } = "";
    public string UserId { get; set; } = "";
    public DateTimeOffset Expires { get; set; }
    [ConcurrencyCheck]
    public string? ConsumedByHash { get; set; }
}

public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshRequest(string Token);

public sealed class DevelopmentTokenIssuer(TimeProvider clock)
{
    public byte[] Key { get; } = RandomNumberGenerator.GetBytes(32);
    public const string Issuer = "AdventureWorks.LocalTraining";
    public const string Audience = "AdventureWorks.Api";

    public string Issue(ApplicationUser user)
    {
        var now = clock.GetUtcNow();
        var token = new JwtSecurityToken(Issuer, Audience,
            new[] { new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Name, user.Email ?? user.Id) },
            now.UtcDateTime, now.AddMinutes(15).UtcDateTime,
            new SigningCredentials(new SymmetricSecurityKey(Key), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string NewRefreshToken() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(token)));
}

public sealed class LibraryClaimsTransformation(UserManager<ApplicationUser> users) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true || principal.HasClaim(c => c.Type == "library_card"))
            return principal;
        var id = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id is null) return principal;
        var user = await users.FindByIdAsync(id);
        if (user is null || string.IsNullOrEmpty(user.LibraryCardNumber)) return principal;
        var clone = principal.Clone();
        clone.AddIdentity(new ClaimsIdentity(new[] { new Claim("library_card", user.LibraryCardNumber) }));
        return clone;
    }
}
