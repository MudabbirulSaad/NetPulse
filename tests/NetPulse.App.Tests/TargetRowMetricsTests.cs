using NetPulse.App.ViewModels;
using NetPulse.Core.Models;

namespace NetPulse.App.Tests;

public sealed class TargetRowMetricsTests
{
    [Fact]
    public void RowCalculatesRollingStatisticsAndFailureGaps()
    {
        var target = new MonitorTarget(
            Guid.NewGuid(),
            "Metrics",
            TargetType.Http,
            "https://example.com/",
            null,
            10,
            5,
            true,
            DateTimeOffset.UnixEpoch);
        var history = new[]
        {
            Result(target.Id, HealthState.Healthy, 10),
            Result(target.Id, HealthState.Offline, 0, ProbeErrorCode.Timeout),
            Result(target.Id, HealthState.Degraded, 30),
        };
        var row = new TargetRowViewModel(new TargetSnapshot(
            target,
            HealthState.Degraded,
            history[^1],
            history));

        Assert.Equal("10 ms", row.MinimumLatency);
        Assert.Equal("20 ms", row.AverageLatency);
        Assert.Equal("30 ms", row.MaximumLatency);
        Assert.Equal(new double?[] { 10, null, 30 }, row.GraphPoints);
    }

    [Fact]
    public void RowUsesPlaceholderWhenNoLatencyExists()
    {
        var target = new MonitorTarget(
            Guid.NewGuid(),
            "Offline",
            TargetType.Http,
            "https://example.com/",
            null,
            10,
            5,
            true,
            DateTimeOffset.UnixEpoch);
        var result = Result(
            target.Id,
            HealthState.Offline,
            0,
            ProbeErrorCode.ConnectionRefused);
        var row = new TargetRowViewModel(new TargetSnapshot(
            target,
            HealthState.Offline,
            result,
            [result]));

        Assert.Equal("—", row.MinimumLatency);
        Assert.Equal("—", row.AverageLatency);
        Assert.Equal("—", row.MaximumLatency);
        Assert.Equal(new double?[] { null }, row.GraphPoints);
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
