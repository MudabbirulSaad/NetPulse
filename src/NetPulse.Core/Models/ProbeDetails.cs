using System.Text.Json.Serialization;

namespace NetPulse.Core.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(HttpProbeDetails), "http")]
[JsonDerivedType(typeof(DnsProbeDetails), "dns")]
public abstract record ProbeDetails;

public sealed record HttpProbeDetails(
    int? StatusCode,
    string? ReasonPhrase,
    string? FinalUrl) : ProbeDetails;

public sealed record DnsProbeDetails(
    IReadOnlyList<string> Addresses,
    IcmpResult? Icmp) : ProbeDetails;

public sealed record IcmpResult(
    bool WasAttempted,
    bool IsSuccessful,
    TimeSpan? RoundTripTime,
    string Message);
