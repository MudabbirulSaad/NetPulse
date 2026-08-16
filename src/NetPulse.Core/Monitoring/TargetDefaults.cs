using NetPulse.Core.Models;

namespace NetPulse.Core.Monitoring;

public static class TargetDefaults
{
    public static IReadOnlyList<TargetDraft> Create() =>
    [
        new(
            Name: "OpenReels",
            Type: TargetType.Http,
            Address: "https://openreels.com/",
            DnsResolver: null,
            PollIntervalSeconds: 60,
            TimeoutSeconds: 10),
        new(
            Name: "Google DNS",
            Type: TargetType.Dns,
            Address: "google.com",
            DnsResolver: "8.8.8.8",
            PollIntervalSeconds: 30,
            TimeoutSeconds: 5),
        new(
            Name: "Cloudflare DNS",
            Type: TargetType.Dns,
            Address: "cloudflare.com",
            DnsResolver: "1.1.1.1",
            PollIntervalSeconds: 30,
            TimeoutSeconds: 5),
    ];
}
