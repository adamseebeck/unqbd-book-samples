using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Correlation;
using Xunit;

namespace ContosoSupportCopilot.Tests;

// The direction an AsyncLocal value flows, which the correlation middleware depends on.
public class CorrelationContextTests
{
    [Fact]
    public async Task AValueSetInASynchronousMethod_StaysSetForTheCaller_AndFlowsIntoAwaitedCode()
    {
        var context = new CorrelationContext();

        SetInASynchronousMethod(context, "request-1");

        Assert.Equal("request-1", context.CorrelationId);
        Assert.Equal("request-1", await ReadAfterAnAwaitAsync(context));
    }

    [Fact]
    public async Task AValueSetInAnAsyncMethod_DoesNotFlowBackToTheCaller()
    {
        var context = new CorrelationContext();
        context.SetCorrelationId("outer");

        await SetInAnAsyncMethodAsync(context, "inner");

        Assert.Equal("outer", context.CorrelationId);
    }

    private static void SetInASynchronousMethod(ICorrelationContext context, string correlationId) =>
        context.SetCorrelationId(correlationId);

    // When an async method returns, the runtime restores the caller's execution context.
    private static async Task SetInAnAsyncMethodAsync(ICorrelationContext context, string correlationId)
    {
        context.SetCorrelationId(correlationId);
        await Task.Yield();
    }

    private static async Task<string> ReadAfterAnAwaitAsync(ICorrelationContext context)
    {
        await Task.Yield();
        return context.CorrelationId;
    }
}
