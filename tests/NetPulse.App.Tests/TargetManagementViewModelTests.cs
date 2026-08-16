using NetPulse.App.Services;
using NetPulse.App.ViewModels;
using NetPulse.Core.Models;
using NetPulse.Core.Session;

namespace NetPulse.App.Tests;

public sealed class TargetManagementViewModelTests
{
    [Fact]
    public async Task AddCommandAppliesNewTarget()
    {
        var session = new ManagementSession([]);
        var draft = Draft("New");
        var dialogs = new FakeDialogs { EditorResult = draft };
        var viewModel = new DashboardViewModel(session, dialogService: dialogs);
        await viewModel.InitializeAsync();

        await viewModel.AddTargetCommand.ExecuteAsync(null);

        var change = Assert.IsType<TargetChange.Add>(Assert.Single(session.Changes));
        Assert.Equal(draft, change.Draft);
        Assert.Null(dialogs.LastEditorTarget);
    }

    [Fact]
    public async Task EditCommandKeepsSelectedTargetIdentity()
    {
        var snapshot = Snapshot("Original");
        var session = new ManagementSession([snapshot]);
        var dialogs = new FakeDialogs { EditorResult = Draft("Changed") };
        var viewModel = new DashboardViewModel(session, dialogService: dialogs);
        await viewModel.InitializeAsync();

        await viewModel.EditTargetCommand.ExecuteAsync(null);

        var change = Assert.IsType<TargetChange.Update>(Assert.Single(session.Changes));
        Assert.Equal(snapshot.Target.Id, change.TargetId);
        Assert.Equal("Changed", change.Draft.Name);
        Assert.Equal(snapshot.Target.Id, dialogs.LastEditorTarget!.Id);
    }

    [Fact]
    public async Task DeleteCommandDoesNothingWhenConfirmationIsDeclined()
    {
        var session = new ManagementSession([Snapshot("Keep")]);
        var dialogs = new FakeDialogs { ConfirmDeleteResult = false };
        var viewModel = new DashboardViewModel(session, dialogService: dialogs);
        await viewModel.InitializeAsync();

        await viewModel.DeleteTargetCommand.ExecuteAsync(null);

        Assert.Empty(session.Changes);
    }

    [Fact]
    public async Task ToggleCommandDisablesEnabledTarget()
    {
        var snapshot = Snapshot("Target");
        var session = new ManagementSession([snapshot]);
        var viewModel = new DashboardViewModel(
            session,
            dialogService: new FakeDialogs());
        await viewModel.InitializeAsync();

        await viewModel.ToggleTargetCommand.ExecuteAsync(null);

        var change = Assert.IsType<TargetChange.SetEnabled>(Assert.Single(session.Changes));
        Assert.Equal(snapshot.Target.Id, change.TargetId);
        Assert.False(change.IsEnabled);
    }

    [Fact]
    public async Task SelectedTestUsesCurrentTargetDraft()
    {
        var snapshot = Snapshot("Target");
        var session = new ManagementSession([snapshot]);
        var viewModel = new DashboardViewModel(session);
        await viewModel.InitializeAsync();

        await viewModel.TestSelectedCommand.ExecuteAsync(null);

        Assert.Equal("Target", Assert.Single(session.TestDrafts).Name);
    }

    [Fact]
    public async Task SelectedTestUsesSafeUserFacingError()
    {
        var session = new ManagementSession([Snapshot("Target")])
        {
            RunOnceException = new IOException("secret-path-or-host"),
        };
        var viewModel = new DashboardViewModel(session);
        await viewModel.InitializeAsync();

        await viewModel.TestSelectedCommand.ExecuteAsync(null);

        Assert.Equal(
            "NetPulse could not complete the action. Technical details were written to the local log.",
            viewModel.ErrorMessage);
        Assert.DoesNotContain("secret-path-or-host", viewModel.ErrorMessage, StringComparison.Ordinal);
    }

    private static TargetDraft Draft(string name) =>
        new(name, TargetType.Http, "https://example.com/", null, 10, 5);

    private static TargetSnapshot Snapshot(string name)
    {
        var target = new MonitorTarget(
            Guid.NewGuid(),
            name,
            TargetType.Http,
            "https://example.com/",
            null,
            10,
            5,
            true,
            DateTimeOffset.UnixEpoch);
        return new TargetSnapshot(target, HealthState.Checking, null, []);
    }

    private sealed class FakeDialogs : ITargetDialogService
    {
        public TargetDraft? EditorResult { get; init; }

        public bool ConfirmDeleteResult { get; init; } = true;

        public TargetRowViewModel? LastEditorTarget { get; private set; }

        public TargetDraft? ShowEditor(
            TargetRowViewModel? existingTarget,
            int currentTargetCount)
        {
            LastEditorTarget = existingTarget;
            return EditorResult;
        }

        public bool ConfirmDelete(TargetRowViewModel target) => ConfirmDeleteResult;
    }

    private sealed class ManagementSession(IReadOnlyList<TargetSnapshot> targets)
        : INetPulseSession
    {
        public NetPulseState CurrentState { get; private set; } =
            new(false, false, targets);

        public List<TargetChange> Changes { get; } = [];

        public List<TargetDraft> TestDrafts { get; } = [];

        public Exception? RunOnceException { get; init; }

        public event EventHandler<NetPulseState>? StateChanged;

        public Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            CurrentState = CurrentState with { IsInitialized = true };
            StateChanged?.Invoke(this, CurrentState);
            return Task.CompletedTask;
        }

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            CurrentState = CurrentState with { IsRunning = true };
            StateChanged?.Invoke(this, CurrentState);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<CheckResult> RunOnceAsync(
            TargetDraft target,
            CancellationToken cancellationToken = default)
        {
            TestDrafts.Add(target);
            if (RunOnceException is not null)
            {
                return Task.FromException<CheckResult>(RunOnceException);
            }

            return Task.FromResult(new CheckResult(
                Guid.NewGuid(),
                DateTimeOffset.UnixEpoch,
                TimeSpan.FromMilliseconds(1),
                HealthState.Healthy,
                "Healthy"));
        }

        public Task ApplyAsync(
            TargetChange change,
            CancellationToken cancellationToken = default)
        {
            Changes.Add(change);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
