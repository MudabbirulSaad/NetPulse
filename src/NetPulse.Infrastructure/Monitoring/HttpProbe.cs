using System.Net;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using NetPulse.Core.Models;
using NetPulse.Core.Monitoring;

namespace NetPulse.Infrastructure.Monitoring;

internal sealed class HttpProbe : IProbe, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly TimeProvider _timeProvider;
    private readonly bool _ownsClient;

    public HttpProbe(
        HttpClient httpClient,
        TimeProvider? timeProvider = null,
        bool ownsClient = false)
    {
        ArgumentNullException.ThrowIfNull(httpClient);

        _httpClient = httpClient;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _ownsClient = ownsClient;
        _httpClient.Timeout = Timeout.InfiniteTimeSpan;

        if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
        {
            _httpClient.DefaultRequestHeaders.UserAgent.Add(
                new ProductInfoHeaderValue("NetPulse", "1.0"));
        }
    }

    public TargetType TargetType => TargetType.Http;

    public async Task<CheckResult> CheckAsync(
        MonitorTarget target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);

        if (target.Type != TargetType.Http ||
            !Uri.TryCreate(target.Address, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return Failure(
                target.Id,
                TimeSpan.Zero,
                ProbeErrorCode.InvalidConfiguration,
                "The HTTP target configuration is invalid.");
        }

        var startedAtUtc = _timeProvider.GetUtcNow();
        var startedTimestamp = _timeProvider.GetTimestamp();
        using var timeoutSource = new CancellationTokenSource(
            TimeSpan.FromSeconds(target.TimeoutSeconds),
            _timeProvider);
        using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            timeoutSource.Token);
        using var request = new HttpRequestMessage(HttpMethod.Get, uri);

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                linkedSource.Token).ConfigureAwait(false);

            var duration = _timeProvider.GetElapsedTime(startedTimestamp);
            var statusCode = (int)response.StatusCode;
            var state = HealthClassifier.FromHttpStatusCode(statusCode);
            var message = state == HealthState.Healthy
                ? $"HTTP {statusCode} received."
                : $"HTTP {statusCode} indicates a service problem.";

            return new CheckResult(
                target.Id,
                startedAtUtc,
                duration,
                state,
                message,
                Details: new HttpProbeDetails(
                    statusCode,
                    response.ReasonPhrase,
                    response.RequestMessage?.RequestUri?.AbsoluteUri ?? uri.AbsoluteUri));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            return Failure(
                target.Id,
                _timeProvider.GetElapsedTime(startedTimestamp),
                ProbeErrorCode.Timeout,
                $"The HTTP check exceeded {target.TimeoutSeconds} seconds.",
                startedAtUtc);
        }
        catch (HttpRequestException exception)
        {
            var errorCode = Classify(exception);
            return Failure(
                target.Id,
                _timeProvider.GetElapsedTime(startedTimestamp),
                errorCode,
                MessageFor(errorCode),
                startedAtUtc);
        }
        catch (AuthenticationException)
        {
            return Failure(
                target.Id,
                _timeProvider.GetElapsedTime(startedTimestamp),
                ProbeErrorCode.TlsFailure,
                MessageFor(ProbeErrorCode.TlsFailure),
                startedAtUtc);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return Failure(
                target.Id,
                _timeProvider.GetElapsedTime(startedTimestamp),
                ProbeErrorCode.UnexpectedFailure,
                MessageFor(ProbeErrorCode.UnexpectedFailure),
                startedAtUtc);
        }
    }

    public void Dispose()
    {
        if (_ownsClient)
        {
            _httpClient.Dispose();
        }
    }

    private static ProbeErrorCode Classify(HttpRequestException exception)
    {
        if (exception.HttpRequestError == HttpRequestError.NameResolutionError)
        {
            return ProbeErrorCode.DnsFailure;
        }

        if (exception.HttpRequestError == HttpRequestError.SecureConnectionError ||
            exception.InnerException is AuthenticationException or
                IOException { InnerException: AuthenticationException })
        {
            return ProbeErrorCode.TlsFailure;
        }

        if (exception.HttpRequestError == HttpRequestError.ConnectionError ||
            exception.InnerException is SocketException)
        {
            return ProbeErrorCode.ConnectionRefused;
        }

        return ProbeErrorCode.UnexpectedFailure;
    }

    private static string MessageFor(ProbeErrorCode errorCode) => errorCode switch
    {
        ProbeErrorCode.DnsFailure => "The host name could not be resolved.",
        ProbeErrorCode.ConnectionRefused => "The remote service refused the connection.",
        ProbeErrorCode.TlsFailure => "A secure TLS connection could not be established.",
        _ => "The HTTP check failed unexpectedly.",
    };

    private static CheckResult Failure(
        Guid targetId,
        TimeSpan duration,
        ProbeErrorCode errorCode,
        string message,
        DateTimeOffset? timestampUtc = null) =>
        new(
            targetId,
            timestampUtc ?? DateTimeOffset.UtcNow,
            duration,
            HealthClassifier.FromProbeFailure(errorCode),
            message,
            errorCode,
            new HttpProbeDetails(null, null, null));
}
