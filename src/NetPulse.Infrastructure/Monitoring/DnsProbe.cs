using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using DnsClient;
using NetPulse.Core.Models;

namespace NetPulse.Infrastructure.Monitoring;

internal sealed class DnsProbe(
    IDnsLookup dnsLookup,
    IIcmpProbe icmpProbe,
    TimeProvider? timeProvider = null) : IProbe
{
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public TargetType TargetType => TargetType.Dns;

    public async Task<CheckResult> CheckAsync(
        MonitorTarget target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (target.Type != TargetType.Dns ||
            string.IsNullOrWhiteSpace(target.Address) ||
            target.DnsResolver is null ||
            !IPAddress.TryParse(target.DnsResolver, out _))
        {
            return Result(
                target.Id,
                _timeProvider.GetUtcNow(),
                TimeSpan.Zero,
                HealthState.Error,
                "The DNS target configuration is invalid.",
                ProbeErrorCode.InvalidConfiguration,
                Array.Empty<string>(),
                NotAttemptedIcmp());
        }

        var startedAtUtc = _timeProvider.GetUtcNow();
        var startedTimestamp = _timeProvider.GetTimestamp();
        var timeout = TimeSpan.FromSeconds(target.TimeoutSeconds);
        using var timeoutSource = new CancellationTokenSource(timeout, _timeProvider);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);

        var icmpTask = SafeIcmpAsync(
            target.DnsResolver,
            timeout,
            linkedSource.Token,
            cancellationToken);

        try
        {
            var lookup = await dnsLookup.QueryAAsync(
                target.Address,
                target.DnsResolver,
                timeout,
                linkedSource.Token).ConfigureAwait(false);
            var duration = _timeProvider.GetElapsedTime(startedTimestamp);
            var icmp = await icmpTask.ConfigureAwait(false);

            if (lookup.HasResolverError)
            {
                return Result(
                    target.Id,
                    startedAtUtc,
                    duration,
                    HealthState.Degraded,
                    lookup.ResolverMessage ?? "The resolver returned a DNS error.",
                    ProbeErrorCode.DnsFailure,
                    lookup.Addresses,
                    icmp);
            }

            if (lookup.Addresses.Count == 0)
            {
                return Result(
                    target.Id,
                    startedAtUtc,
                    duration,
                    HealthState.Degraded,
                    "The resolver returned no IPv4 addresses.",
                    ProbeErrorCode.DnsFailure,
                    lookup.Addresses,
                    icmp);
            }

            return Result(
                target.Id,
                startedAtUtc,
                duration,
                HealthState.Healthy,
                $"Resolved {lookup.Addresses.Count} IPv4 address(es).",
                null,
                lookup.Addresses,
                icmp);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            return Offline(
                target.Id,
                startedAtUtc,
                startedTimestamp,
                ProbeErrorCode.Timeout,
                "The DNS query timed out.");
        }
        catch (TimeoutException)
        {
            return Offline(
                target.Id,
                startedAtUtc,
                startedTimestamp,
                ProbeErrorCode.Timeout,
                "The DNS query timed out.");
        }
        catch (DnsResponseException exception)
            when (exception.Code == DnsResponseCode.ConnectionTimeout)
        {
            return Offline(
                target.Id,
                startedAtUtc,
                startedTimestamp,
                ProbeErrorCode.Timeout,
                "The DNS resolver did not respond before the timeout.");
        }
        catch (Exception exception)
            when (exception is SocketException or PingException or DnsResponseException)
        {
            return Offline(
                target.Id,
                startedAtUtc,
                startedTimestamp,
                ProbeErrorCode.ConnectionRefused,
                "The DNS resolver could not be reached.");
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Result(
                target.Id,
                startedAtUtc,
                _timeProvider.GetElapsedTime(startedTimestamp),
                HealthState.Error,
                "The DNS check failed unexpectedly.",
                ProbeErrorCode.UnexpectedFailure,
                Array.Empty<string>(),
                NotAttemptedIcmp());
        }
    }

    private async Task<IcmpResult> SafeIcmpAsync(
        string address,
        TimeSpan timeout,
        CancellationToken linkedToken,
        CancellationToken callerToken)
    {
        try
        {
            return await icmpProbe.CheckAsync(address, timeout, linkedToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (callerToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return new IcmpResult(
                WasAttempted: true,
                IsSuccessful: false,
                RoundTripTime: null,
                Message: "ICMP was blocked or unavailable.");
        }
    }

    private CheckResult Offline(
        Guid targetId,
        DateTimeOffset timestampUtc,
        long startedTimestamp,
        ProbeErrorCode errorCode,
        string message) =>
        Result(
            targetId,
            timestampUtc,
            _timeProvider.GetElapsedTime(startedTimestamp),
            HealthState.Offline,
            message,
            errorCode,
            Array.Empty<string>(),
            NotAttemptedIcmp());

    private static CheckResult Result(
        Guid targetId,
        DateTimeOffset timestampUtc,
        TimeSpan duration,
        HealthState state,
        string message,
        ProbeErrorCode? errorCode,
        IReadOnlyList<string> addresses,
        IcmpResult icmp) =>
        new(
            targetId,
            timestampUtc,
            duration,
            state,
            message,
            errorCode,
            new DnsProbeDetails(addresses, icmp));

    private static IcmpResult NotAttemptedIcmp() =>
        new(false, false, null, "ICMP was not completed.");
}
