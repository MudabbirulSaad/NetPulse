using System.Net;
using DnsClient;

namespace NetPulse.Infrastructure.Monitoring;

internal sealed class DnsClientLookup : IDnsLookup
{
    public async Task<DnsLookupOutcome> QueryAAsync(
        string domain,
        string resolver,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var options = CreateOptions(IPAddress.Parse(resolver), timeout);
        var client = new LookupClient(options);
        var response = await client.QueryAsync(
            domain,
            QueryType.A,
            QueryClass.IN,
            cancellationToken).ConfigureAwait(false);
        var addresses = response.Answers
            .ARecords()
            .Select(static record => record.Address.ToString())
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return new DnsLookupOutcome(
            response.HasError,
            response.HasError ? response.ErrorMessage : null,
            addresses);
    }

    internal static LookupClientOptions CreateOptions(
        IPAddress resolver,
        TimeSpan timeout) =>
        new(resolver)
        {
            AutoResolveNameServers = false,
            UseCache = false,
            Retries = 0,
            Timeout = timeout,
            ThrowDnsErrors = false,
        };
}
