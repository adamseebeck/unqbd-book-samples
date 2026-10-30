using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace ContosoSupportCopilot.Infrastructure.Security;

// Chapter 11 read the engineer from the X-Engineer-Id header, which any caller can set. Outside Development,
// the engineer is the signed-in user of a Microsoft Entra ID access token that the JwtBearer handler validated:
// its object ID (oid) claim, when the token's scp claim grants the API's Copilot.Use scope. An app-only token
// has no scp claim, so a service principal never passes for an engineer. Development keeps the header for local
// runs and the chapter check.
public sealed class EngineerIdentity
{
    public const string HeaderName = "X-Engineer-Id";
    public const string ObjectIdClaim = "oid";
    public const string ScopeClaim = "scp";
    public const string RequiredScope = "Copilot.Use";

    private readonly IHostEnvironment _environment;

    public EngineerIdentity(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public string? GetEngineerId(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // scp lists the delegated scopes, separated by spaces.
            var scopes = context.User.FindFirst(ScopeClaim)?.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
            return scopes.Contains(RequiredScope, StringComparer.Ordinal) ? context.User.FindFirst(ObjectIdClaim)?.Value : null;
        }

        var header = context.Request.Headers[HeaderName].FirstOrDefault();
        return _environment.IsDevelopment() && !string.IsNullOrWhiteSpace(header) ? header : null;
    }
}
