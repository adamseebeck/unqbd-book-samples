using System.ClientModel;
using System.ClientModel.Primitives;
using System.Globalization;
using System.Reflection;
using ContosoSupportCopilot.Infrastructure.Resilience;
using Moq;
using Moq.Protected;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public sealed class RetryHeaderTests
{
    [Fact]
    public void RetryAfterAcceptsFutureHttpDate_WithinTheCallBudget()
    {
        Assert.Equal(TimeSpan.FromSeconds(120),DelayFor("Retry-After",DateTimeOffset.UtcNow.AddHours(1).ToString("R",CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void RetryAfterExpiredHttpDateDoesNotRequestANegativeWait()
    {
        Assert.Equal(TimeSpan.Zero,DelayFor("Retry-After","Sat, 01 Jan 2000 00:00:00 GMT"));
    }

    [Theory]
    [InlineData("retry-after-ms","125.5",125.5)]
    [InlineData("Retry-After","2",2000.0)]
    [InlineData("Retry-After","9999",120000.0)]
    [InlineData("retry-after-ms","1e30",120000.0)]
    public void RetryAfterHonorsUnitsWithoutExceedingTheBudget(string name,string value,double expectedMilliseconds)
    {
        Assert.Equal(TimeSpan.FromMilliseconds(expectedMilliseconds),DelayFor(name,value));
    }

    [Theory]
    [InlineData("retry-after-ms","NaN")]
    [InlineData("retry-after-ms","-1")]
    [InlineData("Retry-After","not-a-date")]
    public void InvalidRetryAfterKeepsTheNormalBackoff(string name,string value)
    {
        Assert.Null(DelayFor(name,value));
    }

    [Theory]
    [InlineData(400,false)]
    [InlineData(401,false)]
    [InlineData(403,false)]
    [InlineData(404,false)]
    [InlineData(408,true)]
    [InlineData(429,true)]
    [InlineData(503,true)]
    public void ResilienceRetriesOnlyTransientHttpFailures(int status,bool transient)
    {
        Assert.Equal(transient,KernelResiliencePipeline.IsTransient(new HttpRequestException("Synthetic",null,(System.Net.HttpStatusCode)status),CancellationToken.None));
    }

    private static TimeSpan? DelayFor(string name,string value)
    {
        var headers=new Mock<PipelineResponseHeaders>();
        string? headerValue=value;
        headers.Setup(h=>h.TryGetValue(name,out headerValue)).Returns(true);
        var response=new Mock<PipelineResponse>();
        response.SetupGet(r=>r.Status).Returns(429);
        response.Protected().SetupGet<PipelineResponseHeaders>("HeadersCore").Returns(headers.Object);
        var exception=new ClientResultException("Synthetic throttle",response.Object);
        var method=typeof(KernelResiliencePipeline).GetMethod("RetryAfter",BindingFlags.NonPublic|BindingFlags.Static)!;
        object?[] arguments=[exception,TimeSpan.FromSeconds(120)];
        return (TimeSpan?)method.Invoke(null,arguments);
    }
}
