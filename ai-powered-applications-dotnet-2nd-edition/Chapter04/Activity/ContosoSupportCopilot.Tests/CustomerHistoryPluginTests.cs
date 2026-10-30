using ContosoSupportCopilot.Domain.Services;
using ContosoSupportCopilot.Infrastructure.Plugins;
using Moq;
using Xunit;

namespace ContosoSupportCopilot.Tests;

public class CustomerHistoryPluginTests
{
    [Fact]
    public async Task GetRecentTicketCountAsync_CountsTicketsFromTheLast30Days()
    {
        var customerId = Guid.NewGuid();
        var supportContext = new Mock<ISupportContext>();
        supportContext
            .Setup(c => c.CountTicketsSinceAsync(customerId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        var plugin = new CustomerHistoryPlugin(supportContext.Object);

        var count = await plugin.GetRecentTicketCountAsync(customerId);

        Assert.Equal(2, count);
        var expectedStart = DateTime.UtcNow.AddDays(-30);
        supportContext.Verify(c => c.CountTicketsSinceAsync(
            customerId,
            It.Is<DateTime>(d => d > expectedStart.AddMinutes(-1) && d < expectedStart.AddMinutes(1)),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
