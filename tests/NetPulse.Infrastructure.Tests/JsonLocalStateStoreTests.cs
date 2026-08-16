using NetPulse.Core.Models;
using NetPulse.Infrastructure.Monitoring;
using NetPulse.Infrastructure.Session;
using NetPulse.Infrastructure.Storage;

namespace NetPulse.Infrastructure.Tests;

public sealed class JsonLocalStateStoreTests
{
    [Fact]
    public async Task RoundTripPreservesTargetsAndTypedHistory()
    {
        using var directory = new TemporaryDirectory();
        var paths = LocalStatePaths.FromRoot(directory.Path);
        var store = new JsonLocalStateStore(paths);
        var target = Target();
        var check = new CheckResult(
            target.Id,
            DateTimeOffset.Parse(
                "2026-08-16T00:00:00Z",
                System.Globalization.CultureInfo.InvariantCulture),
            TimeSpan.FromMilliseconds(42),
            HealthState.Healthy,
            "HTTP 200 received.",
            Details: new HttpProbeDetails(200, "OK", target.Address));
        var state = new StoredLocalState(
            [target],
            new Dictionary<Guid, IReadOnlyList<CheckResult>> { [target.Id] = [check] });

        await store.SaveAsync(state, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(target, Assert.Single(loaded.Targets));
        var loadedCheck = Assert.Single(loaded.History[target.Id]);
        Assert.Equal(check.Duration, loadedCheck.Duration);
        Assert.IsType<HttpProbeDetails>(loadedCheck.Details);
        Assert.Null(loaded.Warning);
    }

    [Fact]
    public async Task MissingFilesRequestFirstRunDefaults()
    {
        using var directory = new TemporaryDirectory();
        var store = new JsonLocalStateStore(LocalStatePaths.FromRoot(directory.Path));

        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.Null(loaded);
    }

    [Fact]
    public async Task CorruptSettingsAreRenamedAndDefaultsRequested()
    {
        using var directory = new TemporaryDirectory();
        var paths = LocalStatePaths.FromRoot(directory.Path);
        Directory.CreateDirectory(paths.RootDirectory);
        await File.WriteAllTextAsync(paths.SettingsFile, "{not json");
        var store = new JsonLocalStateStore(
            paths,
            new FixedTimeProvider(new DateTimeOffset(2026, 8, 16, 3, 4, 5, TimeSpan.Zero)));

        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.True(loaded.ShouldSeedDefaults);
        Assert.Contains("Default targets were restored", loaded.Warning, StringComparison.Ordinal);
        Assert.False(File.Exists(paths.SettingsFile));
        Assert.True(File.Exists(Path.Combine(
            paths.RootDirectory,
            "settings.corrupt-20260816-030405000.json")));
    }

    [Fact]
    public async Task CorruptHistoryIsRenamedWithoutReplacingTargets()
    {
        using var directory = new TemporaryDirectory();
        var paths = LocalStatePaths.FromRoot(directory.Path);
        var store = new JsonLocalStateStore(paths);
        var target = Target();
        await store.SaveAsync(
            new StoredLocalState(
                [target],
                new Dictionary<Guid, IReadOnlyList<CheckResult>>()),
            CancellationToken.None);
        await File.WriteAllTextAsync(paths.HistoryFile, "[broken");

        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.False(loaded.ShouldSeedDefaults);
        Assert.Equal(target, Assert.Single(loaded.Targets));
        Assert.Empty(loaded.History);
        Assert.Contains("Monitoring history was reset", loaded.Warning, StringComparison.Ordinal);
        Assert.Single(Directory.GetFiles(paths.RootDirectory, "history.corrupt-*.json"));
    }

    [Fact]
    public async Task SaveTrimsHistoryAndLeavesNoTemporaryFiles()
    {
        using var directory = new TemporaryDirectory();
        var paths = LocalStatePaths.FromRoot(directory.Path);
        var store = new JsonLocalStateStore(paths);
        var target = Target();
        var history = Enumerable.Range(1, 105)
            .Select(index => new CheckResult(
                target.Id,
                DateTimeOffset.UnixEpoch.AddSeconds(index),
                TimeSpan.FromMilliseconds(index),
                HealthState.Healthy,
                "Healthy"))
            .ToArray();

        await store.SaveAsync(
            new StoredLocalState(
                [target],
                new Dictionary<Guid, IReadOnlyList<CheckResult>> { [target.Id] = history }),
            CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        Assert.NotNull(loaded);
        Assert.Equal(100, loaded.History[target.Id].Count);
        Assert.Equal(TimeSpan.FromMilliseconds(6), loaded.History[target.Id][0].Duration);
        Assert.Empty(Directory.GetFiles(paths.RootDirectory, "*.tmp"));
    }

    [Fact]
    public async Task DefaultsAreSeededOnlyOnFirstRun()
    {
        using var directory = new TemporaryDirectory();
        var paths = LocalStatePaths.FromRoot(directory.Path);
        var store = new JsonLocalStateStore(paths);
        await using (var firstSession = Session(store))
        {
            await firstSession.InitializeAsync();
            Assert.Equal(3, firstSession.CurrentState.Targets.Count);

            foreach (var target in firstSession.CurrentState.Targets.ToArray())
            {
                await firstSession.ApplyAsync(new TargetChange.Delete(target.Target.Id));
            }
        }

        await using var restartedSession = Session(new JsonLocalStateStore(paths));
        await restartedSession.InitializeAsync();

        Assert.Empty(restartedSession.CurrentState.Targets);
    }

    private static NetPulseSession Session(ILocalStateStore store) =>
        new(
            [new StubProbe(TargetType.Http), new StubProbe(TargetType.Dns)],
            store);

    private static MonitorTarget Target() =>
        new(
            Guid.Parse("44beeb8c-028f-402b-a9ea-4952813dcce2"),
            "Example",
            TargetType.Http,
            "https://example.com/",
            null,
            10,
            5,
            true,
            DateTimeOffset.UnixEpoch);

    private sealed class StubProbe(TargetType type) : IProbe
    {
        public TargetType TargetType => type;

        public Task<CheckResult> CheckAsync(
            MonitorTarget target,
            CancellationToken cancellationToken) =>
            Task.FromResult(new CheckResult(
                target.Id,
                DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds(1),
                HealthState.Healthy,
                "Healthy"));
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
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
