using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.IdentityModel.Tokens;

namespace AdventureWorks;

// Signs short-lived access tokens for the mobile client. The key
// comes from Jwt:SigningKey (User Secrets locally, a secret store
// in deployment); without one, each start uses a random key, so
// tokens never outlive the process.
public sealed class TokenIssuer(TimeProvider clock,
    IConfiguration configuration)
{
    public const string Issuer = "AdventureWorks";
    public const string Audience = "AdventureWorks.Api";

    public SymmetricSecurityKey Key { get; } = new(
        configuration["Jwt:SigningKey"] is { Length: >= 32 } key
            ? Encoding.UTF8.GetBytes(key)
            : RandomNumberGenerator.GetBytes(32));

    public string Issue(ApplicationUser user)
    {
        var now = clock.GetUtcNow();
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Id)
        ];
        var signing = new SigningCredentials(
            Key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(Issuer, Audience, claims,
            now.UtcDateTime, now.AddMinutes(15).UtcDateTime,
            signing);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public static string NewRefreshToken() =>
        Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) =>
        Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
