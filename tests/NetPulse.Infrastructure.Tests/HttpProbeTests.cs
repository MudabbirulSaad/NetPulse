using System.Net;
using System.Net.Sockets;
using NetPulse.Core.Models;
using NetPulse.Infrastructure.Monitoring;

namespace NetPulse.Infrastructure.Tests;

public sealed class HttpProbeTests
{
    [Theory]
    [InlineData(HttpStatusCode.OK, HealthState.Healthy)]
    [InlineData(HttpStatusCode.Found, HealthState.Healthy)]
    [InlineData(HttpStatusCode.NotFound, HealthState.Degraded)]
    [InlineData(HttpStatusCode.InternalServerError, HealthState.Degraded)]
    public async Task StatusCodeMapsToExpectedHealth(
        HttpStatusCode statusCode,
        HealthState expected)
    {
        using var handler = new StubHandler((request, _) => Task.FromResult(
            new HttpResponseMessage(statusCode)
            {
                RequestMessage = request,
                ReasonPhrase = statusCode.ToString(),
            }));
        using var client = new HttpClient(handler);
        using var probe = new HttpProbe(client);

        var result = await probe.CheckAsync(Target(), CancellationToken.None);

        Assert.Equal(expected, result.State);
        Assert.Null(result.ErrorCode);
        var details = Assert.IsType<HttpProbeDetails>(result.Details);
        Assert.Equal((int)statusCode, details.StatusCode);
    }

    [Fact]
    public async Task RequestUsesNetPulseUserAgent()
    {
        string? userAgent = null;
        using var handler = new StubHandler((request, _) =>
        {
            userAgent = request.Headers.UserAgent.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });
        using var client = new HttpClient(handler);
        using var probe = new HttpProbe(client);

        await probe.CheckAsync(Target(), CancellationToken.None);

        Assert.Equal("NetPulse/1.0", userAgent);
    }

    [Fact]
    public async Task HardTimeoutReturnsOfflineResult()
    {
        using var handler = new StubHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);
        using var probe = new HttpProbe(client);

        var result = await probe.CheckAsync(Target(timeoutSeconds: 1), CancellationToken.None);

        Assert.Equal(HealthState.Offline, result.State);
        Assert.Equal(ProbeErrorCode.Timeout, result.ErrorCode);
    }

    [Theory]
    [InlineData(HttpRequestError.NameResolutionError, ProbeErrorCode.DnsFailure)]
    [InlineData(HttpRequestError.SecureConnectionError, ProbeErrorCode.TlsFailure)]
    [InlineData(HttpRequestError.ConnectionError, ProbeErrorCode.ConnectionRefused)]
    public async Task RequestFailureReturnsTypedOfflineResult(
        HttpRequestError requestError,
        ProbeErrorCode expected)
    {
        using var handler = new StubHandler((_, _) => Task.FromException<HttpResponseMessage>(
            new HttpRequestException(requestError, "Expected test failure")));
        using var client = new HttpClient(handler);
        using var probe = new HttpProbe(client);

        var result = await probe.CheckAsync(Target(), CancellationToken.None);

        Assert.Equal(HealthState.Offline, result.State);
        Assert.Equal(expected, result.ErrorCode);
    }

    [Fact]
    public async Task SocketFailureReturnsConnectionRefusedResult()
    {
        using var handler = new StubHandler((_, _) => Task.FromException<HttpResponseMessage>(
            new HttpRequestException(
                "Expected test failure",
                new SocketException((int)SocketError.ConnectionRefused))));
        using var client = new HttpClient(handler);
        using var probe = new HttpProbe(client);

        var result = await probe.CheckAsync(Target(), CancellationToken.None);

        Assert.Equal(HealthState.Offline, result.State);
        Assert.Equal(ProbeErrorCode.ConnectionRefused, result.ErrorCode);
    }

    [Fact]
    public async Task CallerCancellationPropagatesWithoutOfflineResult()
    {
        using var handler = new StubHandler(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);
        using var probe = new HttpProbe(client);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            probe.CheckAsync(Target(), cancellationSource.Token));
    }

    [Fact]
    public async Task InvalidConfigurationReturnsErrorResult()
    {
        using var handler = new StubHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));
        using var client = new HttpClient(handler);
        using var probe = new HttpProbe(client);

        var result = await probe.CheckAsync(
            Target(address: "not-a-url"),
            CancellationToken.None);

        Assert.Equal(HealthState.Error, result.State);
        Assert.Equal(ProbeErrorCode.InvalidConfiguration, result.ErrorCode);
        Assert.Equal(0, handler.CallCount);
    }

    private static MonitorTarget Target(
        string address = "https://example.com/health",
        int timeoutSeconds = 5) =>
        new(
            Guid.NewGuid(),
            "Example",
            TargetType.Http,
            address,
            null,
            10,
            timeoutSeconds,
            true,
            DateTimeOffset.UnixEpoch);

    private sealed class StubHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> response)
        : HttpMessageHandler
    {
        public int CallCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            CallCount++;
            return response(request, cancellationToken);
        }
    }
}
