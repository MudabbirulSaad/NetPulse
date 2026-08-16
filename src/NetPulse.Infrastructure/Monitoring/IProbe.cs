using NetPulse.Core.Models;

namespace NetPulse.Infrastructure.Monitoring;

internal interface IProbe
{
    TargetType TargetType { get; }

    Task<CheckResult> CheckAsync(
        MonitorTarget target,
        CancellationToken cancellationToken);
}
