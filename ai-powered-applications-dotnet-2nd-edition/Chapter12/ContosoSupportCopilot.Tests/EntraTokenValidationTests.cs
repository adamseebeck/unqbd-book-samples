using System.Security.Cryptography;
using ContosoSupportCopilot.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

// The token settings from deploy/kubernetes.yaml, applied to tokens shaped like the v2.0 access tokens that
// Microsoft Entra ID issues for an API whose app registration sets requestedAccessTokenVersion to 2: aud is
// the API's client ID and iss ends in /v2.0. The test signs them with its own RSA key, which stands in for the
// keys that the Authority's metadata publishes.
public sealed class EntraTokenValidationTests : IDisposable
{
    private const string TenantId = "11111111-2222-3333-4444-555555555555";
    private const string ApiClientId = "66666666-7777-8888-9999-000000000000";
    private const string V2Issuer = $"https://login.microsoftonline.com/{TenantId}/v2.0";
    private const string EngineerObjectId = "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee";

    private readonly RSA _rsa = RSA.Create(2048);
    private readonly RsaSecurityKey _signingKey;
    private readonly ServiceProvider _services;

    public EntraTokenValidationTests()
    {
        _signingKey = new RsaSecurityKey(_rsa) { KeyId = "test-signing-key" };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Schemes:Bearer:ValidIssuer"] = V2Issuer,
            ["Authentication:Schemes:Bearer:ValidAudiences:0"] = ApiClientId
        }).Build();

        // The registration from Program.cs, with the test key in place of the Authority's metadata.
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddLogging();
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters.IssuerSigningKey = _signingKey;
            });
        _services = services.BuildServiceProvider();
    }

    [Fact]
    public async Task AV2TokenForTheApiClientId_WithTheCopilotScope_NamesTheEngineerByObjectId()
    {
        var context = await AuthenticateAsync(CreateToken(audience: ApiClientId, scope: "Copilot.Use"));

        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.Equal(EngineerObjectId, ProductionIdentity().GetEngineerId(context));
    }

    [Fact]
    public async Task ATokenForTheApplicationIdUri_OrFromTheV1Issuer_IsRejected()
    {
        var applicationIdUri = await AuthenticateAsync(CreateToken(audience: "api://contoso-support-copilot", scope: "Copilot.Use"));
        var v1Issuer = await AuthenticateAsync(CreateToken(audience: ApiClientId, scope: "Copilot.Use",
            issuer: $"https://sts.windows.net/{TenantId}/"));

        Assert.False(applicationIdUri.User.Identity?.IsAuthenticated);
        Assert.False(v1Issuer.User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task AnAppOnlyToken_IsValid_ButNamesNoEngineer()
    {
        var context = await AuthenticateAsync(CreateToken(audience: ApiClientId, scope: null, role: "Copilot.Process"));

        Assert.True(context.User.Identity?.IsAuthenticated);
        Assert.Null(ProductionIdentity().GetEngineerId(context));
    }

    private string CreateToken(string audience, string? scope, string issuer = V2Issuer, string? role = null)
    {
        var claims = new Dictionary<string, object> { ["oid"] = EngineerObjectId, ["tid"] = TenantId, ["ver"] = "2.0" };
        if (scope is not null)
        {
            claims["scp"] = scope;
        }

        if (role is not null)
        {
            claims["roles"] = new[] { role };
        }

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer,
            Audience = audience,
            Claims = claims,
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.RsaSha256)
        });
    }

    private async Task<HttpContext> AuthenticateAsync(string token)
    {
        var context = new DefaultHttpContext { RequestServices = _services };
        context.Request.Headers.Authorization = $"Bearer {token}";
        var result = await context.AuthenticateAsync(JwtBearerDefaults.AuthenticationScheme);
        if (result.Succeeded)
        {
            context.User = result.Principal!;
        }

        return context;
    }

    private static EngineerIdentity ProductionIdentity()
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);
        return new EngineerIdentity(environment.Object);
    }

    public void Dispose()
    {
        _services.Dispose();
        _rsa.Dispose();
    }
}
