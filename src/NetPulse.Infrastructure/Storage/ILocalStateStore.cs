using NetPulse.Core.Models;

namespace NetPulse.Infrastructure.Storage;

internal interface ILocalStateStore
{
    Task<StoredLocalState?> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(StoredLocalState state, CancellationToken cancellationToken);
}

internal sealed record StoredLocalState(
    IReadOnlyList<MonitorTarget> Targets,
    IReadOnlyDictionary<Guid, IReadOnlyList<CheckResult>> History)
{
    public static StoredLocalState Empty { get; } = new(
        Array.Empty<MonitorTarget>(),
        new Dictionary<Guid, IReadOnlyList<CheckResult>>());
}
