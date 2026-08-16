using NetPulse.Core.Models;
using NetPulse.Infrastructure.Monitoring;
using NetPulse.Infrastructure.Session;
using NetPulse.Infrastructure.Storage;

namespace NetPulse.Infrastructure.Tests;

public sealed class NetPulseSessionTests
{
    [Fact]
    public async Task InitializeLoadsTargetsAndHistory()
    {
        var target = HttpTarget();
        var result = HealthyResult(target.Id);
        var store = new MemoryStateStore(new StoredLocalState(
            [target],
            new Dictionary<Guid, IReadOnlyList<CheckResult>>
            {
                [target.Id] = [result],
            }));
        await using var session = Session(store, new ImmediateProbe(TargetType.Http));

        await session.InitializeAsync();

        Assert.True(session.CurrentState.IsInitialized);
        var snapshot = Assert.Single(session.CurrentState.Targets);
        Assert.Equal(HealthState.Healthy, snapshot.State);
        Assert.Equal(result, snapshot.LatestResult);
    }

    [Fact]
    public async Task SchedulerAndRunOnceNeverOverlapForSameTarget()
    {
        var target = HttpTarget();
        var store = StoreWith(target);
        var probe = new BlockingProbe(TargetType.Http);
        await using var session = Session(store, probe);

        await session.StartAsync();
        await probe.FirstCallStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var runOnce = session.RunOnceAsync(DraftFor(target));

        await Task.Delay(50);
        Assert.Equal(1, probe.CallCount);
        probe.ReleaseFirstCall.SetResult();
        await runOnce.WaitAsync(TimeSpan.FromSeconds(2));
        await session.StopAsync();

        Assert.Equal(1, probe.MaximumConcurrency);
        Assert.Equal(2, probe.CallCount);
    }

    [Fact]
    public async Task FailureInOneTargetDoesNotStopAnotherTarget()
    {
        var failingTarget = HttpTarget(name: "Failing");
        var healthyTarget = DnsTarget();
        var store = StoreWith(failingTarget, healthyTarget);
        var httpProbe = new ThrowingProbe(TargetType.Http);
        var dnsProbe = new ImmediateProbe(TargetType.Dns);
        await using var session = new NetPulseSession(
            [httpProbe, dnsProbe],
            store);

        await session.StartAsync();
        await WaitUntilAsync(
            () => session.CurrentState.Targets.All(static target => target.History.Count > 0));
        await session.StopAsync();

        Assert.Equal(
            HealthState.Error,
            session.CurrentState.Targets.Single(target => target.Target.Id == failingTarget.Id).State);
        Assert.Equal(
            HealthState.Healthy,
            session.CurrentState.Targets.Single(target => target.Target.Id == healthyTarget.Id).State);
    }

    [Fact]
    public async Task StopCancelsActiveCheckWithoutCreatingOfflineResult()
    {
        var target = HttpTarget();
        var existing = HealthyResult(target.Id);
        var store = new MemoryStateStore(new StoredLocalState(
            [target],
            new Dictionary<Guid, IReadOnlyList<CheckResult>> { [target.Id] = [existing] }));
        var probe = new CancellationProbe(TargetType.Http);
        await using var session = Session(store, probe);

        await session.StartAsync();
        await probe.Started.Task.WaitAsync(TimeSpan.FromSeconds(2));
        await session.StopAsync();

        var snapshot = Assert.Single(session.CurrentState.Targets);
        Assert.Equal(HealthState.Healthy, snapshot.State);
        Assert.Single(snapshot.History);
        Assert.DoesNotContain(snapshot.History, result => result.State == HealthState.Offline);
    }

    [Fact]
    public async Task ApplySupportsAddUpdateDisableAndDelete()
    {
        var store = new MemoryStateStore(StoredLocalState.Empty);
        await using var session = Session(store, new ImmediateProbe(TargetType.Http));
        await session.InitializeAsync();

        await session.ApplyAsync(new TargetChange.Add(
            new TargetDraft("New", TargetType.Http, "https://example.com", null)));
        var added = Assert.Single(session.CurrentState.Targets).Target;

        await session.ApplyAsync(new TargetChange.Update(
            added.Id,
            new TargetDraft("Changed", TargetType.Http, "https://example.org", null)));
        Assert.Equal("Changed", Assert.Single(session.CurrentState.Targets).Target.Name);

        await session.ApplyAsync(new TargetChange.SetEnabled(added.Id, false));
        Assert.False(Assert.Single(session.CurrentState.Targets).Target.IsEnabled);

        await session.ApplyAsync(new TargetChange.Delete(added.Id));
        Assert.Empty(session.CurrentState.Targets);
        Assert.True(store.SaveCount >= 4);
    }

    [Fact]
    public async Task DisabledTargetDoesNotStartUntilEnabled()
    {
        var target = HttpTarget(isEnabled: false);
        var probe = new ImmediateProbe(TargetType.Http);
        await using var session = Session(StoreWith(target), probe);

        await session.StartAsync();
        await Task.Delay(50);
        Assert.Equal(0, probe.CallCount);

        await session.ApplyAsync(new TargetChange.SetEnabled(target.Id, true));
        await WaitUntilAsync(() => probe.CallCount == 1);
        await session.StopAsync();
    }

    [Fact]
    public async Task InvalidRunOnceReturnsConfigurationError()
    {
        await using var session = Session(
            new MemoryStateStore(StoredLocalState.Empty),
            new ImmediateProbe(TargetType.Http));

        var result = await session.RunOnceAsync(
            new TargetDraft("Invalid", TargetType.Http, "relative", null));

        Assert.Equal(HealthState.Error, result.State);
        Assert.Equal(ProbeErrorCode.InvalidConfiguration, result.ErrorCode);
    }

    private static NetPulseSession Session(
        MemoryStateStore store,
        IProbe httpProbe) =>
        new(
            [httpProbe, new ImmediateProbe(TargetType.Dns)],
            store);

    private static MemoryStateStore StoreWith(params MonitorTarget[] targets) =>
        new(new StoredLocalState(
            targets,
            new Dictionary<Guid, IReadOnlyList<CheckResult>>()));

    private static MonitorTarget HttpTarget(
        string name = "HTTP",
        bool isEnabled = true) =>
        new(
            Guid.NewGuid(),
            name,
            TargetType.Http,
            "https://example.com/",
            null,
            5,
            2,
            isEnabled,
            DateTimeOffset.UnixEpoch);

    private static MonitorTarget DnsTarget() =>
        new(
            Guid.NewGuid(),
            "DNS",
            TargetType.Dns,
            "example.com",
            "1.1.1.1",
            5,
            2,
            true,
            DateTimeOffset.UnixEpoch.AddSeconds(1));

    private static TargetDraft DraftFor(MonitorTarget target) =>
        new(
            target.Name,
            target.Type,
            target.Address,
            target.DnsResolver,
            target.PollIntervalSeconds,
            target.TimeoutSeconds,
            target.IsEnabled);

    private static CheckResult HealthyResult(Guid targetId) =>
        new(
            targetId,
            DateTimeOffset.UnixEpoch,
            TimeSpan.FromMilliseconds(10),
            HealthState.Healthy,
            "Healthy");

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        while (!condition())
        {
            await Task.Delay(10, timeout.Token);
        }
    }

    private sealed class MemoryStateStore(StoredLocalState? initial) : ILocalStateStore
    {
        public int SaveCount { get; private set; }

        public StoredLocalState? State { get; private set; } = initial;

        public Task<StoredLocalState?> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(State);

        public Task SaveAsync(StoredLocalState state, CancellationToken cancellationToken)
        {
            SaveCount++;
            State = state;
            return Task.CompletedTask;
        }
    }

    private sealed class ImmediateProbe(TargetType targetType) : IProbe
    {
        private int _callCount;

        public TargetType TargetType => targetType;

        public int CallCount => Volatile.Read(ref _callCount);

        public Task<CheckResult> CheckAsync(
            MonitorTarget target,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _callCount);
            return Task.FromResult(HealthyResult(target.Id));
        }
    }

    private sealed class ThrowingProbe(TargetType targetType) : IProbe
    {
        public TargetType TargetType => targetType;

        public Task<CheckResult> CheckAsync(
            MonitorTarget target,
            CancellationToken cancellationToken) =>
            throw new InvalidOperationException("Expected test failure");
    }

    private sealed class CancellationProbe(TargetType targetType) : IProbe
    {
        public TargetType TargetType => targetType;

        public TaskCompletionSource Started { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<CheckResult> CheckAsync(
            MonitorTarget target,
            CancellationToken cancellationToken)
        {
            Started.TrySetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return HealthyResult(target.Id);
        }
    }

    private sealed class BlockingProbe(TargetType targetType) : IProbe
    {
        private int _active;
        private int _callCount;
        private int _maximumConcurrency;

        public TargetType TargetType => targetType;

        public int CallCount => Volatile.Read(ref _callCount);

        public int MaximumConcurrency => Volatile.Read(ref _maximumConcurrency);

        public TaskCompletionSource FirstCallStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseFirstCall { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<CheckResult> CheckAsync(
            MonitorTarget target,
            CancellationToken cancellationToken)
        {
            var call = Interlocked.Increment(ref _callCount);
            var active = Interlocked.Increment(ref _active);
            InterlockedExtensions.Max(ref _maximumConcurrency, active);

            try
            {
                if (call == 1)
                {
                    FirstCallStarted.TrySetResult();
                    await ReleaseFirstCall.Task.WaitAsync(cancellationToken);
                }

                return HealthyResult(target.Id);
            }
            finally
            {
                Interlocked.Decrement(ref _active);
            }
        }
    }

    private static class InterlockedExtensions
    {
        public static void Max(ref int location, int value)
        {
            var current = Volatile.Read(ref location);
            while (current < value)
            {
                var original = Interlocked.CompareExchange(ref location, value, current);
                if (original == current)
                {
                    return;
                }

                current = original;
            }
        }
    }
}
