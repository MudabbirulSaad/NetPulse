namespace NetPulse.Core.Models;

public sealed record TargetSnapshot(
    MonitorTarget Target,
    HealthState State,
    CheckResult? LatestResult,
    IReadOnlyList<CheckResult> History);
