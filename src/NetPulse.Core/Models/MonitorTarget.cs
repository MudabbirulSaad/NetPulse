namespace NetPulse.Core.Models;

public sealed record MonitorTarget(
    Guid Id,
    string Name,
    TargetType Type,
    string Address,
    string? DnsResolver,
    int PollIntervalSeconds,
    int TimeoutSeconds,
    bool IsEnabled,
    DateTimeOffset CreatedAtUtc);
