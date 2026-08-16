namespace NetPulse.Core.Models;

public sealed record NetPulseState(
    bool IsInitialized,
    bool IsRunning,
    IReadOnlyList<TargetSnapshot> Targets,
    string? Warning = null)
{
    public static NetPulseState Empty { get; } = new(
        IsInitialized: false,
        IsRunning: false,
        Targets: Array.Empty<TargetSnapshot>());
}
