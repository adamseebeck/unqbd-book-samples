using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AdventureWorks.Infrastructure.Identity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace AdventureWorks;

// Signs short-lived access tokens for the mobile client. The key
// comes from Jwt:SigningKey (User Secrets locally, a secret store
// in deployment); without one, each start uses a random key, so
// tokens never outlive the process. A key under 32 characters is
// a mistake, not a choice, so it fails instead of being ignored.
public sealed class TokenIssuer(TimeProvider clock,
    IConfiguration configuration)
{
    public const string Issuer = "AdventureWorks";
    public const string Audience = "AdventureWorks.Api";

    public SymmetricSecurityKey Key { get; } = new(
        configuration["Jwt:SigningKey"] switch
        {
            null => RandomNumberGenerator.GetBytes(32),
            { Length: >= 32 } key => Encoding.UTF8.GetBytes(key),
            _ => throw new InvalidOperationException(
                "Jwt:SigningKey needs 32 or more characters.")
        });

    public string Issue(ApplicationUser user)
    {
        var now = clock.GetUtcNow();
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Id)
        ];
        return new JsonWebTokenHandler().CreateToken(
            new SecurityTokenDescriptor
            {
                Issuer = Issuer,
                Audience = Audience,
                Subject = new ClaimsIdentity(claims),
                NotBefore = now.UtcDateTime,
                Expires = now.AddMinutes(15).UtcDateTime,
                SigningCredentials = new SigningCredentials(
                    Key, SecurityAlgorithms.HmacSha256)
            });
    }

    public static string NewRefreshToken() =>
        Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(32));

    public static string Hash(string token) =>
        Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
