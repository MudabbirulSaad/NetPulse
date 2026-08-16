using NetPulse.Core.Models;
using NetPulse.Core.Monitoring;

namespace NetPulse.Core.Tests;

public sealed class HealthClassifierTests
{
    [Theory]
    [InlineData(200, HealthState.Healthy)]
    [InlineData(302, HealthState.Healthy)]
    [InlineData(404, HealthState.Degraded)]
    [InlineData(500, HealthState.Degraded)]
    public void HttpStatusMapsToExpectedHealth(int statusCode, HealthState expected)
    {
        Assert.Equal(expected, HealthClassifier.FromHttpStatusCode(statusCode));
    }

    [Theory]
    [InlineData(ProbeErrorCode.Timeout, HealthState.Offline)]
    [InlineData(ProbeErrorCode.DnsFailure, HealthState.Offline)]
    [InlineData(ProbeErrorCode.ConnectionRefused, HealthState.Offline)]
    [InlineData(ProbeErrorCode.TlsFailure, HealthState.Offline)]
    [InlineData(ProbeErrorCode.InvalidConfiguration, HealthState.Error)]
    [InlineData(ProbeErrorCode.UnexpectedFailure, HealthState.Error)]
    [InlineData(ProbeErrorCode.Cancellation, HealthState.Checking)]
    public void ProbeFailureMapsToExpectedHealth(ProbeErrorCode code, HealthState expected)
    {
        Assert.Equal(expected, HealthClassifier.FromProbeFailure(code));
    }

    [Fact]
    public void ResolverResponseErrorIsDegraded()
    {
        Assert.Equal(HealthState.Degraded, HealthClassifier.FromDnsResolverError());
    }
}
