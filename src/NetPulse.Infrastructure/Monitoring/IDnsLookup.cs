namespace NetPulse.Infrastructure.Monitoring;

internal interface IDnsLookup
{
    Task<DnsLookupOutcome> QueryAAsync(
        string domain,
        string resolver,
        TimeSpan timeout,
        CancellationToken cancellationToken);
}

internal sealed record DnsLookupOutcome(
    bool HasResolverError,
    string? ResolverMessage,
    IReadOnlyList<string> Addresses);
