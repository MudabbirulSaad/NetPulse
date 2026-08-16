using System.Collections.Concurrent;
using NetPulse.Core.Models;
using NetPulse.Infrastructure.Logging;
using NetPulse.Infrastructure.Monitoring;
using NetPulse.Infrastructure.Session;
using NetPulse.Infrastructure.Storage;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace NetPulse.Infrastructure.Tests;

public sealed class ReliabilityTests
{
    [Fact]
    public async Task FailureLogContainsTechnicalContextWithoutTargetAddress()
    {
        var target = Target("https://example.com/?token=sensitive-value");
        var store = new TestStore(new StoredLocalState(
            [target],
            new Dictionary<Guid, IReadOnlyList<CheckResult>>()));
        var sink = new CapturingSink();
        using var logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Sink(sink)
            .CreateLogger();
        await using var session = new NetPulseSession(
            [new OfflineProbe(TargetType.Http), new OfflineProbe(TargetType.Dns)],
            store,
            logger: logger);

        await session.StartAsync();
        await WaitUntilAsync(() => session.CurrentState.Targets[0].History.Count == 1);
        await session.StopAsync();

        var logText = string.Join(
            Environment.NewLine,
            sink.Events.Select(static item =>
                $"{item.RenderMessage(System.Globalization.CultureInfo.InvariantCulture)} " +
                string.Join(
                    " ",
                    item.Properties.Select(static property =>
                        $"{property.Key}={property.Value}"))));
        Assert.Contains(target.Id.ToString(), logText, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Offline", logText, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-value", logText, StringComparison.Ordinal);
        Assert.DoesNotContain(target.Address, logText, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ResultPersistenceFailureDoesNotStopStateUpdates()
    {
        var target = Target("https://example.com/");
        var store = new TestStore(
            new StoredLocalState(
                [target],
                new Dictionary<Guid, IReadOnlyList<CheckResult>>()),
            failSaves: true);
        var sink = new CapturingSink();
        using var logger = new LoggerConfiguration()
            .WriteTo.Sink(sink)
            .CreateLogger();
        await using var session = new NetPulseSession(
            [new HealthyProbe(TargetType.Http), new HealthyProbe(TargetType.Dns)],
            store,
            logger: logger);

        await session.StartAsync();
        await WaitUntilAsync(() => session.CurrentState.Targets[0].History.Count == 1);
        await session.StopAsync();

        var snapshot = Assert.Single(session.CurrentState.Targets);
        Assert.Equal(HealthState.Healthy, snapshot.State);
        Assert.Contains("could not be saved", session.CurrentState.Warning, StringComparison.Ordinal);
        Assert.Contains(
            sink.Events,
            item => item.RenderMessage(
                System.Globalization.CultureInfo.InvariantCulture).Contains(
                "Result persistence failed",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task ConfigurationPersistenceFailureUsesSafeMessage()
    {
        var store = new TestStore(StoredLocalState.Empty, failSaves: true);
        using var logger = new LoggerConfiguration().CreateLogger();
        await using var session = new NetPulseSession(
            [new HealthyProbe(TargetType.Http), new HealthyProbe(TargetType.Dns)],
            store,
            logger: logger);
        await session.InitializeAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            session.ApplyAsync(new TargetChange.Add(new TargetDraft(
                "Site",
                TargetType.Http,
                "https://example.com/",
                null))));

        Assert.Equal("Target changes could not be saved.", exception.Message);
        Assert.DoesNotContain("path", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DailyFileLoggerCreatesReadableRollingFile()
    {
        using var directory = new TemporaryDirectory();
        var paths = LocalStatePaths.FromRoot(directory.Path);

        using (var logger = NetPulseLogging.Create(paths))
        {
            logger.Information(
                "Reliability event. EventCode={EventCode}",
                "NP_TEST");
        }

        var logFile = Assert.Single(Directory.GetFiles(
            paths.LogsDirectory,
            "netpulse-*.log"));
        var content = File.ReadAllText(logFile);
        Assert.Contains("Reliability event", content, StringComparison.Ordinal);
        Assert.Contains("NP_TEST", content, StringComparison.Ordinal);
    }

    private static MonitorTarget Target(string address) =>
        new(
            Guid.NewGuid(),
            "Sensitive target",
            TargetType.Http,
            address,
            null,
            5,
            2,
            true,
            DateTimeOffset.UnixEpoch);

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private sealed class CapturingSink : ILogEventSink
    {
        public ConcurrentQueue<LogEvent> Events { get; } = [];

        public void Emit(LogEvent logEvent)
        {
            Events.Enqueue(logEvent);
        }
    }

    private sealed class TestStore(
        StoredLocalState initial,
        bool failSaves = false) : ILocalStateStore
    {
        public Task<StoredLocalState?> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult<StoredLocalState?>(initial);

        public Task SaveAsync(StoredLocalState state, CancellationToken cancellationToken) =>
            failSaves
                ? Task.FromException(new IOException("Expected test failure"))
                : Task.CompletedTask;
    }

    private sealed class HealthyProbe(TargetType targetType) : IProbe
    {
        public TargetType TargetType => targetType;

        public Task<CheckResult> CheckAsync(
            MonitorTarget target,
            CancellationToken cancellationToken) =>
            Task.FromResult(new CheckResult(
                target.Id,
                DateTimeOffset.UnixEpoch,
                TimeSpan.FromMilliseconds(5),
                HealthState.Healthy,
                "Healthy"));
    }

    private sealed class OfflineProbe(TargetType targetType) : IProbe
    {
        public TargetType TargetType => targetType;

        public Task<CheckResult> CheckAsync(
            MonitorTarget target,
            CancellationToken cancellationToken) =>
            Task.FromResult(new CheckResult(
                target.Id,
                DateTimeOffset.UnixEpoch,
                TimeSpan.FromMilliseconds(5),
                HealthState.Offline,
                "Offline",
                ProbeErrorCode.ConnectionRefused));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "NetPulse.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
