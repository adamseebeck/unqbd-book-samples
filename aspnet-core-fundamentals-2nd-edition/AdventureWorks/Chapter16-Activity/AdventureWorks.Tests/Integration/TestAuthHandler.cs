using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AdventureWorks.Tests.Integration;

// Signs a request in as the account named in X-Test-User. Without
// the header the request is anonymous, so the app's fallback
// policy still answers 401. Registered only by StoreFactory.
public sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger, UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(
        options, logger, encoder)
{
    public const string SchemeName = "Test";
    public const string Header = "X-Test-User";

    protected override Task<AuthenticateResult>
        HandleAuthenticateAsync()
    {
        var id = Request.Headers[Header].ToString();
        if (id.Length == 0)
            return Task.FromResult(AuthenticateResult.NoResult());
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, id),
            new(ClaimTypes.Name, id)
        ];
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(claims, SchemeName));
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(principal, SchemeName)));
    }
}
