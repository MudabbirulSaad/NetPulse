using NetPulse.Core.Models;
using NetPulse.Core.Metrics;

namespace NetPulse.App.ViewModels;

public sealed class TargetRowViewModel(TargetSnapshot snapshot)
{
    private readonly LatencyStatistics _statistics = MetricCalculator.Calculate(snapshot.History);

    public TargetSnapshot Snapshot { get; } = snapshot;

    public Guid Id => Snapshot.Target.Id;

    public string Name => Snapshot.Target.Name;

    public string TypeLabel => Snapshot.Target.Type == TargetType.Http ? "HTTP" : "DNS A";

    public string Address => Snapshot.Target.Address;

    public string EndpointCaption => Snapshot.Target.Type == TargetType.Dns
        ? $"{Snapshot.Target.Address}  via  {Snapshot.Target.DnsResolver}"
        : Snapshot.Target.Address;

    public HealthState State => Snapshot.State;

    public string StateLabel => Snapshot.State.ToString().ToUpperInvariant();

    public bool IsEnabled => Snapshot.Target.IsEnabled;

    public string EnabledLabel => IsEnabled ? "ENABLED" : "DISABLED";

    public string StatusMessage => Snapshot.LatestResult?.Message ?? "Waiting for the first sample.";

    public string CurrentLatency => Snapshot.LatestResult is { HasLatency: true } result
        ? $"{result.Duration.TotalMilliseconds:0} ms"
        : "—";

    public string LastChecked => Snapshot.LatestResult is null
        ? "Not checked yet"
        : Snapshot.LatestResult.TimestampUtc.ToLocalTime().ToString(
            "dd MMM, HH:mm:ss",
            System.Globalization.CultureInfo.CurrentCulture);

    public string IcmpStatus => Snapshot.LatestResult?.Details is DnsProbeDetails { Icmp: not null } dns
        ? dns.Icmp.IsSuccessful
            ? $"Reply in {dns.Icmp.RoundTripTime?.TotalMilliseconds:0} ms"
            : dns.Icmp.Message
        : "Not applicable";

    public int SampleCount => Snapshot.History.Count;

    public string MinimumLatency => FormatLatency(_statistics.Minimum);

    public string AverageLatency => FormatLatency(_statistics.Average);

    public string MaximumLatency => FormatLatency(_statistics.Maximum);

    public IReadOnlyList<double?> GraphPoints =>
        MetricCalculator.CreateGraphPoints(Snapshot.History);

    public TargetDraft ToDraft() =>
        new(
            Snapshot.Target.Name,
            Snapshot.Target.Type,
            Snapshot.Target.Address,
            Snapshot.Target.DnsResolver,
            Snapshot.Target.PollIntervalSeconds,
            Snapshot.Target.TimeoutSeconds,
            Snapshot.Target.IsEnabled);

    private static string FormatLatency(TimeSpan? value) => value.HasValue
        ? $"{value.Value.TotalMilliseconds:0} ms"
        : "—";
}
