using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace WarehouseApi.Tests.Infrastructure;

// Base64 carries the subject exactly, including Unicode and trailing spaces, through a test header.
public class SubjectTestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "SubjectTest";
    public const string SubjectHeader = "X-Test-Subject-Base64";
    // A nonempty prefix keeps an empty subject distinct from an omitted HTTP header.
    public const string SubjectPrefix = "b64:";
    public const string RolesHeader = "X-Test-Subject-Roles";
    public const string WarehouseHeader = "X-Test-Subject-Warehouse";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(RolesHeader, out var roles))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var claims = new List<Claim>();
        if (Request.Headers.TryGetValue(SubjectHeader, out var encodedSubject))
        {
            var value = encodedSubject.ToString();
            if (!value.StartsWith(SubjectPrefix, StringComparison.Ordinal))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid test subject encoding."));
            }

            claims.Add(new Claim(ClaimTypes.NameIdentifier,
                Encoding.UTF8.GetString(Convert.FromBase64String(value[SubjectPrefix.Length..]))));
        }

        foreach (var role in roles.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (Request.Headers.TryGetValue(WarehouseHeader, out var warehouse))
        {
            claims.Add(new Claim("WarehouseId", warehouse.ToString()));
        }

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
    }
}
