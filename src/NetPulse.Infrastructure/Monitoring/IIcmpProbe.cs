using NetPulse.Core.Models;

namespace NetPulse.Infrastructure.Monitoring;

internal interface IIcmpProbe
{
    Task<IcmpResult> CheckAsync(
        string address,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}
