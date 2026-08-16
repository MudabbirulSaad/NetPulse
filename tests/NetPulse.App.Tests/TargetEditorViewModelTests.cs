using NetPulse.App.ViewModels;
using NetPulse.Core.Models;
using NetPulse.Core.Session;

namespace NetPulse.App.Tests;

public sealed class TargetEditorViewModelTests
{
    [Fact]
    public void InvalidHttpDraftProducesFieldErrors()
    {
        var viewModel = new TargetEditorViewModel(
            new ProbeSession(HealthState.Healthy),
            existingTarget: null,
            currentTargetCount: 25)
        {
            Name = string.Empty,
            Address = "relative/path",
            PollIntervalSeconds = 5,
            TimeoutText = "5",
        };

        var draft = viewModel.BuildValidatedDraft();

        Assert.Null(draft);
        Assert.NotNull(viewModel.NameError);
        Assert.NotNull(viewModel.AddressError);
        Assert.NotNull(viewModel.TimingError);
        Assert.NotNull(viewModel.GeneralError);
    }

    [Fact]
    public void ValidDnsDraftIsNormalized()
    {
        var viewModel = new TargetEditorViewModel(
            new ProbeSession(HealthState.Healthy),
            existingTarget: null,
            currentTargetCount: 0)
        {
            Name = "  Unicode DNS ",
            TargetType = TargetType.Dns,
            Address = "Bücher.de.",
            DnsResolver = "2001:4860:4860::8888",
            PollIntervalSeconds = 10,
            TimeoutText = "5",
        };

        var draft = viewModel.BuildValidatedDraft();

        Assert.NotNull(draft);
        Assert.Equal("Unicode DNS", draft.Name);
        Assert.Equal("xn--bcher-kva.de", draft.Address);
        Assert.Equal("2001:4860:4860::8888", draft.DnsResolver);
    }

    [Fact]
    public async Task TestConnectionPassesNormalizedDraftToSession()
    {
        var session = new ProbeSession(HealthState.Healthy);
        var viewModel = new TargetEditorViewModel(session, null, 0)
        {
            Name = "Site",
            Address = " https://example.com/health ",
        };

        var result = await viewModel.TestConnectionAsync();

        Assert.NotNull(result);
        Assert.Equal("https://example.com/health", session.LastDraft!.Address);
        Assert.Equal(HealthState.Healthy, viewModel.TestState);
        Assert.Contains("HEALTHY", viewModel.TestStatus, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(HealthState.Offline, true)]
    [InlineData(HealthState.Error, true)]
    [InlineData(HealthState.Degraded, false)]
    [InlineData(HealthState.Healthy, false)]
    public async Task SaveOutcomeFlagsUnreachableService(
        HealthState state,
        bool warningExpected)
    {
        var viewModel = new TargetEditorViewModel(
            new ProbeSession(state),
            null,
            0)
        {
            Name = "Site",
            Address = "https://example.com",
        };

        var result = await viewModel.PrepareSaveAsync();

        Assert.NotNull(result);
        Assert.Equal(warningExpected, result.RequiresUnreachableWarning);
    }

    private sealed class ProbeSession(HealthState resultState) : INetPulseSession
    {
        public NetPulseState CurrentState { get; } = NetPulseState.Empty;

        public TargetDraft? LastDraft { get; private set; }

        public event EventHandler<NetPulseState>? StateChanged
        {
            add { }
            remove { }
        }

        public Task InitializeAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task StartAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<CheckResult> RunOnceAsync(
            TargetDraft target,
            CancellationToken cancellationToken = default)
        {
            LastDraft = target;
            return Task.FromResult(new CheckResult(
                Guid.NewGuid(),
                DateTimeOffset.UnixEpoch,
                TimeSpan.FromMilliseconds(5),
                resultState,
                "Connection result",
                resultState is HealthState.Offline or HealthState.Error
                    ? ProbeErrorCode.ConnectionRefused
                    : null));
        }

        public Task ApplyAsync(
            TargetChange change,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
