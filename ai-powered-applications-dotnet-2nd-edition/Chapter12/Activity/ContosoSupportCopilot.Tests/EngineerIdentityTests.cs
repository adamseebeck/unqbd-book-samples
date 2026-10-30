using System.Security.Claims;
using ContosoSupportCopilot.Infrastructure.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public sealed class EngineerIdentityTests
{
    [Fact]
    public void OutsideDevelopment_TheHeaderNamesNobody()
    {
        Assert.Null(Identity(Environments.Production).GetEngineerId(Request("engineer-ana")));
    }

    [Fact]
    public void ASignedInUser_IsTheObjectIdClaim_WhateverTheHeaderSays()
    {
        var context = SignedIn(Request("engineer-ben"), new Claim("scp", "User.Read Copilot.Use"));

        Assert.Equal("engineer-ana", Identity(Environments.Production).GetEngineerId(context));
        Assert.Equal("engineer-ana", Identity(Environments.Development).GetEngineerId(context));
    }

    [Fact]
    public void AValidTokenWithoutTheCopilotScope_NamesNobody()
    {
        // An app-only token carries roles and no scp claim; a user token may grant other scopes only.
        var appOnly = SignedIn(Request(null), new Claim("roles", "Copilot.Process"));
        var otherScope = SignedIn(Request(null), new Claim("scp", "User.Read"));

        Assert.Null(Identity(Environments.Production).GetEngineerId(appOnly));
        Assert.Null(Identity(Environments.Production).GetEngineerId(otherScope));
    }

    [Fact]
    public void InDevelopment_TheHeaderStillNamesTheEngineer()
    {
        Assert.Equal("engineer-ana", Identity(Environments.Development).GetEngineerId(Request("engineer-ana")));
        Assert.Null(Identity(Environments.Development).GetEngineerId(Request(null)));
    }

    private static EngineerIdentity Identity(string environmentName)
    {
        var environment = new Mock<IHostEnvironment>();
        environment.SetupGet(e => e.EnvironmentName).Returns(environmentName);
        return new EngineerIdentity(environment.Object);
    }

    private static DefaultHttpContext SignedIn(DefaultHttpContext context, Claim permission)
    {
        context.User = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(EngineerIdentity.ObjectIdClaim, "engineer-ana"), permission], authenticationType: "Bearer"));
        return context;
    }

    private static DefaultHttpContext Request(string? header)
    {
        var context = new DefaultHttpContext();
        if (header is not null)
        {
            context.Request.Headers[EngineerIdentity.HeaderName] = header;
        }

        return context;
    }
}
