using System.Net.Sockets;
using DnsClient;
using NetPulse.Core.Models;
using NetPulse.Infrastructure.Monitoring;

namespace NetPulse.Infrastructure.Tests;

public sealed class DnsProbeTests
{
    [Fact]
    public async Task SuccessfulDnsStaysHealthyWhenIcmpIsBlocked()
    {
        var lookup = new StubDnsLookup(new DnsLookupOutcome(
            false,
            null,
            ["93.184.216.34"]));
        var icmp = new StubIcmpProbe(BlockedIcmp());
        var probe = new DnsProbe(lookup, icmp);

        var result = await probe.CheckAsync(Target(), CancellationToken.None);

        Assert.Equal(HealthState.Healthy, result.State);
        Assert.Null(result.ErrorCode);
        var details = Assert.IsType<DnsProbeDetails>(result.Details);
        Assert.Equal("93.184.216.34", Assert.Single(details.Addresses));
        Assert.False(details.Icmp!.IsSuccessful);
        Assert.Equal("1.1.1.1", lookup.Resolver);
    }

    [Fact]
    public async Task NxdomainIsDegradedBecauseResolverResponded()
    {
        var lookup = new StubDnsLookup(new DnsLookupOutcome(
            true,
            "Non-Existent Domain",
            Array.Empty<string>()));
        var probe = new DnsProbe(lookup, new StubIcmpProbe(SuccessfulIcmp()));

        var result = await probe.CheckAsync(Target(), CancellationToken.None);

        Assert.Equal(HealthState.Degraded, result.State);
        Assert.Equal(ProbeErrorCode.DnsFailure, result.ErrorCode);
        Assert.True(result.HasLatency);
    }

    [Fact]
    public async Task EmptyAnswerIsDegraded()
    {
        var lookup = new StubDnsLookup(new DnsLookupOutcome(
            false,
            null,
            Array.Empty<string>()));
        var probe = new DnsProbe(lookup, new StubIcmpProbe(SuccessfulIcmp()));

        var result = await probe.CheckAsync(Target(), CancellationToken.None);

        Assert.Equal(HealthState.Degraded, result.State);
        Assert.Equal(ProbeErrorCode.DnsFailure, result.ErrorCode);
    }

    [Fact]
    public async Task TimeoutIsOffline()
    {
        var lookup = new StubDnsLookup(new TimeoutException("Expected test failure"));
        var probe = new DnsProbe(lookup, new StubIcmpProbe(BlockedIcmp()));

        var result = await probe.CheckAsync(Target(), CancellationToken.None);

        Assert.Equal(HealthState.Offline, result.State);
        Assert.Equal(ProbeErrorCode.Timeout, result.ErrorCode);
    }

    [Fact]
    public async Task UnreachableResolverIsOffline()
    {
        var lookup = new StubDnsLookup(
            new SocketException((int)SocketError.HostUnreachable));
        var probe = new DnsProbe(lookup, new StubIcmpProbe(BlockedIcmp()));

        var result = await probe.CheckAsync(Target(), CancellationToken.None);

        Assert.Equal(HealthState.Offline, result.State);
        Assert.Equal(ProbeErrorCode.ConnectionRefused, result.ErrorCode);
    }

    [Fact]
    public async Task IcmpSuccessIsReportedAsIndependentDetail()
    {
        var lookup = new StubDnsLookup(new DnsLookupOutcome(
            false,
            null,
            ["93.184.216.34"]));
        var probe = new DnsProbe(lookup, new StubIcmpProbe(SuccessfulIcmp()));

        var result = await probe.CheckAsync(Target(), CancellationToken.None);

        var details = Assert.IsType<DnsProbeDetails>(result.Details);
        Assert.True(details.Icmp!.IsSuccessful);
        Assert.Equal(TimeSpan.FromMilliseconds(12), details.Icmp.RoundTripTime);
        Assert.Equal(HealthState.Healthy, result.State);
    }

    [Fact]
    public async Task CallerCancellationPropagates()
    {
        var lookup = new StubDnsLookup(new OperationCanceledException());
        var probe = new DnsProbe(lookup, new StubIcmpProbe(BlockedIcmp()));
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            probe.CheckAsync(Target(), cancellationSource.Token));
    }

    [Fact]
    public async Task InvalidResolverReturnsErrorWithoutCallingAdapters()
    {
        var lookup = new StubDnsLookup(new DnsLookupOutcome(false, null, ["1.2.3.4"]));
        var icmp = new StubIcmpProbe(SuccessfulIcmp());
        var probe = new DnsProbe(lookup, icmp);

        var result = await probe.CheckAsync(
            Target(resolver: "resolver.example"),
            CancellationToken.None);

        Assert.Equal(HealthState.Error, result.State);
        Assert.Equal(ProbeErrorCode.InvalidConfiguration, result.ErrorCode);
        Assert.Equal(0, lookup.CallCount);
        Assert.Equal(0, icmp.CallCount);
    }

    [Fact]
    public async Task LookupFailureCancelsOutstandingIcmpWork()
    {
        var lookup = new StubDnsLookup(
            new SocketException((int)SocketError.HostUnreachable));
        var icmp = new CancellableIcmpProbe();
        var probe = new DnsProbe(lookup, icmp);

        var result = await probe.CheckAsync(Target(), CancellationToken.None);

        Assert.Equal(HealthState.Offline, result.State);
        await icmp.Cancelled.Task.WaitAsync(TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void LookupOptionsDisableCacheAndRetries()
    {
        var options = DnsClientLookup.CreateOptions(
            System.Net.IPAddress.Parse("8.8.8.8"),
            TimeSpan.FromSeconds(7));

        Assert.False(options.AutoResolveNameServers);
        Assert.False(options.UseCache);
        Assert.Equal(0, options.Retries);
        Assert.Equal(TimeSpan.FromSeconds(7), options.Timeout);
        Assert.False(options.ThrowDnsErrors);
    }

    private static MonitorTarget Target(string resolver = "1.1.1.1") =>
        new(
            Guid.NewGuid(),
            "DNS target",
            TargetType.Dns,
            "example.com",
            resolver,
            10,
            5,
            true,
            DateTimeOffset.UnixEpoch);

    private static IcmpResult SuccessfulIcmp() =>
        new(true, true, TimeSpan.FromMilliseconds(12), "Reply received.");

    private static IcmpResult BlockedIcmp() =>
        new(true, false, null, "ICMP blocked.");

    private sealed class StubDnsLookup : IDnsLookup
    {
        private readonly DnsLookupOutcome? _outcome;
        private readonly Exception? _exception;

        public StubDnsLookup(DnsLookupOutcome outcome)
        {
            _outcome = outcome;
        }

        public StubDnsLookup(Exception exception)
        {
            _exception = exception;
        }

        public int CallCount { get; private set; }

        public string? Resolver { get; private set; }

        public Task<DnsLookupOutcome> QueryAAsync(
            string domain,
            string resolver,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            CallCount++;
            Resolver = resolver;

            return _exception is null
                ? Task.FromResult(_outcome!)
                : Task.FromException<DnsLookupOutcome>(_exception);
        }
    }

    private sealed class StubIcmpProbe(IcmpResult result) : IIcmpProbe
    {
        public int CallCount { get; private set; }

        public Task<IcmpResult> CheckAsync(
            string address,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return Task.FromResult(result);
        }
    }

    private sealed class CancellableIcmpProbe : IIcmpProbe
    {
        public TaskCompletionSource Cancelled { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<IcmpResult> CheckAsync(
            string address,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                Cancelled.TrySetResult();
                throw;
            }

            return new IcmpResult(false, false, null, "Not completed");
        }
    }
}
