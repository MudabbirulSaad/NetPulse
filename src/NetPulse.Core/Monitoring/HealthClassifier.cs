using NetPulse.Core.Models;

namespace NetPulse.Core.Monitoring;

public static class HealthClassifier
{
    public static HealthState FromHttpStatusCode(int statusCode) =>
        statusCode is >= 200 and <= 399
            ? HealthState.Healthy
            : HealthState.Degraded;

    public static HealthState FromProbeFailure(ProbeErrorCode errorCode) => errorCode switch
    {
        ProbeErrorCode.InvalidConfiguration => HealthState.Error,
        ProbeErrorCode.UnexpectedFailure => HealthState.Error,
        ProbeErrorCode.Cancellation => HealthState.Checking,
        _ => HealthState.Offline,
    };

    public static HealthState FromDnsResolverError() => HealthState.Degraded;
}
