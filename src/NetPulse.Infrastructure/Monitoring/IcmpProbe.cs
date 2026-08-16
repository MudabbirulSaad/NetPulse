using System.Net;
using System.Net.NetworkInformation;
using NetPulse.Core.Models;

namespace NetPulse.Infrastructure.Monitoring;

internal sealed class IcmpProbe : IIcmpProbe
{
    public async Task<IcmpResult> CheckAsync(
        string address,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using var ping = new Ping();
        var reply = await ping.SendPingAsync(
            IPAddress.Parse(address),
            timeout,
            Array.Empty<byte>(),
            new PingOptions(),
            cancellationToken).ConfigureAwait(false);

        return reply.Status == IPStatus.Success
            ? new IcmpResult(
                WasAttempted: true,
                IsSuccessful: true,
                RoundTripTime: TimeSpan.FromMilliseconds(reply.RoundtripTime),
                Message: "ICMP reply received.")
            : new IcmpResult(
                WasAttempted: true,
                IsSuccessful: false,
                RoundTripTime: null,
                Message: $"ICMP did not succeed ({reply.Status}).");
    }
}
