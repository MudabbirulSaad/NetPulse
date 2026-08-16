namespace NetPulse.Core.Models;

public sealed record TargetDraft(
    string Name,
    TargetType Type,
    string Address,
    string? DnsResolver,
    int PollIntervalSeconds = 10,
    int TimeoutSeconds = 5,
    bool IsEnabled = true);
