namespace NetPulse.Core.Metrics;

public sealed record LatencyStatistics(
    TimeSpan? Minimum,
    TimeSpan? Average,
    TimeSpan? Maximum,
    int SampleCount)
{
    public static LatencyStatistics Empty { get; } = new(null, null, null, 0);
}
