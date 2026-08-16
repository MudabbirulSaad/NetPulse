namespace NetPulse.Core.Models;

public sealed record CheckResult(
    Guid TargetId,
    DateTimeOffset TimestampUtc,
    TimeSpan Duration,
    HealthState State,
    string Message,
    ProbeErrorCode? ErrorCode = null,
    ProbeDetails? Details = null)
{
    public bool HasLatency => State is HealthState.Healthy or HealthState.Degraded;
}
