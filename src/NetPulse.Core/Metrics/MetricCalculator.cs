using NetPulse.Core.Models;

namespace NetPulse.Core.Metrics;

public static class MetricCalculator
{
    public static LatencyStatistics Calculate(IEnumerable<CheckResult> results)
    {
        ArgumentNullException.ThrowIfNull(results);

        var durations = results
            .Where(static result => result.HasLatency)
            .Select(static result => result.Duration)
            .ToArray();

        if (durations.Length == 0)
        {
            return LatencyStatistics.Empty;
        }

        return new LatencyStatistics(
            durations.Min(),
            TimeSpan.FromTicks((long)durations.Average(static value => value.Ticks)),
            durations.Max(),
            durations.Length);
    }

    public static IReadOnlyList<double?> CreateGraphPoints(IEnumerable<CheckResult> results) =>
        results
            .TakeLast(Validation.TargetValidator.MaximumGraphResults)
            .Select(static result => result.HasLatency
                ? result.Duration.TotalMilliseconds
                : (double?)null)
            .ToArray();
}
