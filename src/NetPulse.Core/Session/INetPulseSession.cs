using NetPulse.Core.Models;

namespace NetPulse.Core.Session;

public interface INetPulseSession : IAsyncDisposable
{
    NetPulseState CurrentState { get; }

    event EventHandler<NetPulseState>? StateChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);

    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task<CheckResult> RunOnceAsync(
        TargetDraft target,
        CancellationToken cancellationToken = default);

    Task ApplyAsync(
        TargetChange change,
        CancellationToken cancellationToken = default);
}
