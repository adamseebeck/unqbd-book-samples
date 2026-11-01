using Bunit;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using LibraryDesk.Components;
public sealed class InvestigationTests
{
    [Fact]
    public void CategoryThresholdSuppressesEventsWhileUiStillCounts()
    {
        using var context = new BunitContext();
        var config = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json").AddJsonFile("appsettings.Development.json").Build();
        var sink = new CaptureProvider();
        using var factory = LoggerFactory.Create(logging => logging.AddConfiguration(config.GetSection("Logging")).AddProvider(sink));
        var logger = factory.CreateLogger<CatalogReadRecorder>();
        context.Services.AddSingleton<ILogger<CatalogReadRecorder>>(logger);
        var cut = context.Render<CatalogReadRecorder>();
        cut.Find("button").Click();
        cut.Find("button").Click();
        Assert.Contains("Reads recorded: 2", cut.Markup);
        var suppressed = Environment.GetEnvironmentVariable("ACTIVITY_LOGS_SUPPRESSED") != "false";
        Assert.Equal(!suppressed, logger.IsEnabled(LogLevel.Information));
        Assert.Equal(suppressed ? 0 : 2, sink.Events.Count);
        if (!suppressed)
        {
            Assert.Equal(3, sink.Events[0]["BookCount"]);
            Assert.Equal(1, sink.Events[0]["ReadNumber"]);
            Assert.Equal(2, sink.Events[1]["ReadNumber"]);
        }
    }
    private sealed class CaptureProvider : ILoggerProvider
    {
        public List<Dictionary<string, object?>> Events { get; } = [];
        public ILogger CreateLogger(string name) => new CaptureLogger(Events);
        public void Dispose() { }
        private sealed class CaptureLogger(List<Dictionary<string, object?>> events) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel level) => true;
            public void Log<TState>(LogLevel level, EventId id, TState state, Exception? error, Func<TState, Exception?, string> formatter)
            {
                if (state is IEnumerable<KeyValuePair<string, object?>> values)
                    events.Add(values.ToDictionary(pair => pair.Key, pair => pair.Value));
            }
        }
    }
}
