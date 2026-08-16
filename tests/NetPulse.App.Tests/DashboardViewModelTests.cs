using NetPulse.App.ViewModels;
using NetPulse.Core.Models;
using NetPulse.Core.Session;

namespace NetPulse.App.Tests;

public sealed class DashboardViewModelTests
{
    [Fact]
    public async Task InitializeStartsMonitoringAndBuildsMeasuredSummary()
    {
        var snapshots = new[]
        {
            Snapshot("Healthy", HealthState.Healthy, 20),
            Snapshot("Degraded", HealthState.Degraded, 40),
            Snapshot("Offline", HealthState.Offline, null),
        };
        var session = new FakeSession(snapshots);
        var viewModel = new DashboardViewModel(session);

        await viewModel.InitializeAsync();

        Assert.Equal(1, session.InitializeCount);
        Assert.Equal(1, session.StartCount);
        Assert.True(viewModel.IsRunning);
        Assert.Equal(3, viewModel.TotalTargets);
        Assert.Equal(1, viewModel.HealthyTargets);
        Assert.Equal(2, viewModel.AttentionTargets);
        Assert.Equal("30 ms", viewModel.AverageLatency);
        Assert.Equal("Healthy", viewModel.SelectedTarget!.Name);
    }

    [Fact]
    public async Task ToggleMonitoringStopsAndRestartsSession()
    {
        var session = new FakeSession([Snapshot("Target", HealthState.Healthy, 10)]);
        var viewModel = new DashboardViewModel(session);
        await viewModel.InitializeAsync();

        await viewModel.ToggleMonitoringCommand.ExecuteAsync(null);
        Assert.Equal(1, session.StopCount);
        Assert.False(viewModel.IsRunning);

        await viewModel.ToggleMonitoringCommand.ExecuteAsync(null);
        Assert.Equal(2, session.StartCount);
        Assert.True(viewModel.IsRunning);
    }

    [Fact]
    public async Task RunAllChecksOnlyEnabledTargets()
    {
        var enabled = Snapshot("Enabled", HealthState.Healthy, 10);
        var disabled = Snapshot("Disabled", HealthState.Checking, null, isEnabled: false);
        var session = new FakeSession([enabled, disabled]);
        var viewModel = new DashboardViewModel(session);
        await viewModel.InitializeAsync();

        await viewModel.RunAllNowCommand.ExecuteAsync(null);

        Assert.Equal("Enabled", Assert.Single(session.RunOnceDrafts).Name);
    }

    [Fact]
    public async Task StateChangePreservesSelectedTargetById()
    {
        var first = Snapshot("First", HealthState.Healthy, 10);
        var second = Snapshot("Second", HealthState.Healthy, 20);
        var session = new FakeSession([first, second]);
        var viewModel = new DashboardViewModel(session);
        await viewModel.InitializeAsync();
        viewModel.SelectedTarget = viewModel.Targets[1];

        session.Publish([
            first with { State = HealthState.Degraded },
            second with { State = HealthState.Offline },
        ]);

        Assert.Equal(second.Target.Id, viewModel.SelectedTarget!.Id);
        Assert.Equal(HealthState.Offline, viewModel.SelectedTarget.State);
    }

    [Fact]
    public void DnsRowReportsIcmpWithoutChangingServiceState()
    {
        var target = new MonitorTarget(
            Guid.NewGuid(),
            "DNS",
            TargetType.Dns,
            "example.com",
            "1.1.1.1",
            10,
            5,
            true,
            DateTimeOffset.UnixEpoch);
        var check = new CheckResult(
            target.Id,
            DateTimeOffset.UnixEpoch,
            TimeSpan.FromMilliseconds(12),
            HealthState.Healthy,
            "Resolved",
            Details: new DnsProbeDetails(
                ["93.184.216.34"],
                new IcmpResult(true, false, null, "ICMP blocked.")));
        var row = new TargetRowViewModel(new TargetSnapshot(
            target,
            HealthState.Healthy,
            check,
            [check]));

        Assert.Equal(HealthState.Healthy, row.State);
        Assert.Equal("ICMP blocked.", row.IcmpStatus);
    }

    private static TargetSnapshot Snapshot(
        string name,
        HealthState state,
        int? latencyMilliseconds,
        bool isEnabled = true)
    {
        var target = new MonitorTarget(
            Guid.NewGuid(),
            name,
            TargetType.Http,
            $"https://{name.ToLowerInvariant()}.example.com/",
            null,
            10,
            5,
            isEnabled,
            DateTimeOffset.UnixEpoch);
        var result = latencyMilliseconds is null
            ? new CheckResult(
                target.Id,
                DateTimeOffset.UnixEpoch,
                TimeSpan.Zero,
                state,
                state.ToString(),
                ProbeErrorCode.Timeout)
            : new CheckResult(
                target.Id,
                DateTimeOffset.UnixEpoch,
                TimeSpan.FromMilliseconds(latencyMilliseconds.Value),
                state,
                state.ToString());

        return new TargetSnapshot(target, state, result, [result]);
    }

    private sealed class FakeSession(IReadOnlyList<TargetSnapshot> targets) : INetPulseSession
    {
        public NetPulseState CurrentState { get; private set; } =
            new(false, false, targets);

        public int InitializeCount { get; private set; }

        public int StartCount { get; private set; }

        public int StopCount { get; private set; }

        public List<TargetDraft> RunOnceDrafts { get; } = [];

        public event EventHandler<NetPulseState>? StateChanged;

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            InitializeCount++;
            CurrentState = CurrentState with { IsInitialized = true };
            StateChanged?.Invoke(this, CurrentState);
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            StartCount++;
            CurrentState = CurrentState with { IsRunning = true };
            StateChanged?.Invoke(this, CurrentState);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default)
        {
            StopCount++;
            CurrentState = CurrentState with { IsRunning = false };
            StateChanged?.Invoke(this, CurrentState);
            return Task.CompletedTask;
        }

        public Task<CheckResult> RunOnceAsync(
            TargetDraft target,
            CancellationToken cancellationToken = default)
        {
            RunOnceDrafts.Add(target);
            return Task.FromResult(new CheckResult(
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                TimeSpan.FromMilliseconds(1),
                HealthState.Healthy,
                "Healthy"));
        }

        public Task ApplyAsync(
            TargetChange change,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public void Publish(IReadOnlyList<TargetSnapshot> snapshots)
        {
            CurrentState = CurrentState with { Targets = snapshots };
            StateChanged?.Invoke(this, CurrentState);
        }
    }
}
