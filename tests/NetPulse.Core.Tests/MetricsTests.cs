using NetPulse.Core.Metrics;
using NetPulse.Core.Models;

namespace NetPulse.Core.Tests;

public sealed class MetricsTests
{
    [Fact]
    public void CalculateUsesOnlySuccessfulServiceResponses()
    {
        var targetId = Guid.NewGuid();
        var results = new[]
        {
            Result(targetId, HealthState.Healthy, 10),
            Result(targetId, HealthState.Degraded, 20),
            Result(targetId, HealthState.Offline, 100, ProbeErrorCode.Timeout),
        };

        var statistics = MetricCalculator.Calculate(results);

        Assert.Equal(2, statistics.SampleCount);
        Assert.Equal(TimeSpan.FromMilliseconds(10), statistics.Minimum);
        Assert.Equal(TimeSpan.FromMilliseconds(15), statistics.Average);
        Assert.Equal(TimeSpan.FromMilliseconds(20), statistics.Maximum);
    }

    [Fact]
    public void CreateGraphPointsReturnsLatestThirtyWithFailureGaps()
    {
        var targetId = Guid.NewGuid();
        var results = Enumerable.Range(1, 35)
            .Select(index => index == 34
                ? Result(targetId, HealthState.Offline, index, ProbeErrorCode.Timeout)
                : Result(targetId, HealthState.Healthy, index))
            .ToArray();

        var points = MetricCalculator.CreateGraphPoints(results);

        Assert.Equal(30, points.Count);
        Assert.Equal(6, points[0]);
        Assert.Null(points[28]);
        Assert.Equal(35, points[29]);
    }

    [Fact]
    public void AppendHistoryRetainsLatestOneHundredResults()
    {
        var targetId = Guid.NewGuid();
        IReadOnlyList<CheckResult> history = Array.Empty<CheckResult>();

        foreach (var index in Enumerable.Range(1, 105))
        {
            history = Monitoring.HistoryPolicy.Append(
                history,
                Result(targetId, HealthState.Healthy, index));
        }

        Assert.Equal(100, history.Count);
        Assert.Equal(TimeSpan.FromMilliseconds(6), history[0].Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(105), history[^1].Duration);
    }

    private static CheckResult Result(
        Guid targetId,
        HealthState state,
        int milliseconds,
        ProbeErrorCode? errorCode = null) =>
        new(
            targetId,
            DateTimeOffset.UnixEpoch,
            TimeSpan.FromMilliseconds(milliseconds),
            state,
            state.ToString(),
            errorCode);
}
