using NetPulse.Core.Models;
using NetPulse.Core.Validation;

namespace NetPulse.Core.Tests;

public sealed class TargetValidatorTests
{
    [Fact]
    public void HttpTargetNormalizesWhitespaceAndUrl()
    {
        var draft = HttpDraft(name: "  Public site  ", address: " https://example.com/health ");

        var result = TargetValidator.ValidateAndNormalize(draft, currentTargetCount: 0);

        Assert.True(result.IsValid);
        Assert.Equal("Public site", result.Target!.Name);
        Assert.Equal("https://example.com/health", result.Target.Address);
        Assert.Null(result.Target.DnsResolver);
    }

    [Theory]
    [InlineData("example.com")]
    [InlineData("/health")]
    [InlineData("ftp://example.com")]
    [InlineData("")]
    public void HttpTargetRejectsUnsupportedAddress(string address)
    {
        var result = TargetValidator.ValidateAndNormalize(HttpDraft(address: address), 0);

        Assert.False(result.IsValid);
        Assert.Contains(nameof(TargetDraft.Address), result.Errors.Keys);
    }

    [Fact]
    public void DnsTargetConvertsUnicodeDomainToAscii()
    {
        var draft = new TargetDraft(
            "Unicode domain",
            TargetType.Dns,
            "Bücher.de.",
            "2001:4860:4860::8888",
            10,
            5);

        var result = TargetValidator.ValidateAndNormalize(draft, 0);

        Assert.True(result.IsValid);
        Assert.Equal("xn--bcher-kva.de", result.Target!.Address);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("resolver.example")]
    [InlineData("999.1.1.1")]
    public void DnsTargetRequiresIpResolver(string? resolver)
    {
        var draft = new TargetDraft("DNS", TargetType.Dns, "example.com", resolver, 10, 5);

        var result = TargetValidator.ValidateAndNormalize(draft, 0);

        Assert.False(result.IsValid);
        Assert.Contains(nameof(TargetDraft.DnsResolver), result.Errors.Keys);
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("https://example.com")]
    [InlineData("1.1.1.1")]
    [InlineData("-example.com")]
    public void DnsTargetRejectsInvalidDomain(string domain)
    {
        var draft = new TargetDraft("DNS", TargetType.Dns, domain, "1.1.1.1", 10, 5);

        var result = TargetValidator.ValidateAndNormalize(draft, 0);

        Assert.False(result.IsValid);
        Assert.Contains(nameof(TargetDraft.Address), result.Errors.Keys);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(30)]
    [InlineData(60)]
    public void PollIntervalAcceptsSupportedValues(int interval)
    {
        var result = TargetValidator.ValidateAndNormalize(
            HttpDraft(pollIntervalSeconds: interval, timeoutSeconds: interval == 5 ? 4 : 5),
            0);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(120)]
    public void PollIntervalRejectsUnsupportedValues(int interval)
    {
        var result = TargetValidator.ValidateAndNormalize(
            HttpDraft(pollIntervalSeconds: interval, timeoutSeconds: 1),
            0);

        Assert.False(result.IsValid);
        Assert.Contains(nameof(TargetDraft.PollIntervalSeconds), result.Errors.Keys);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(10, 10)]
    [InlineData(11, 10)]
    [InlineData(31, 60)]
    public void TimeoutRejectsOutOfRangeOrNonShorterValues(int timeout, int interval)
    {
        var result = TargetValidator.ValidateAndNormalize(
            HttpDraft(pollIntervalSeconds: interval, timeoutSeconds: timeout),
            0);

        Assert.False(result.IsValid);
        Assert.Contains(nameof(TargetDraft.TimeoutSeconds), result.Errors.Keys);
    }

    [Fact]
    public void NewTargetRejectsTwentySixthTarget()
    {
        var result = TargetValidator.ValidateAndNormalize(
            HttpDraft(),
            TargetValidator.MaximumTargets);

        Assert.False(result.IsValid);
        Assert.Contains("Targets", result.Errors.Keys);
    }

    [Fact]
    public void ExistingTargetDoesNotCountAgainstMaximum()
    {
        var result = TargetValidator.ValidateAndNormalize(
            HttpDraft(),
            TargetValidator.MaximumTargets,
            isEdit: true);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("                                                             ")]
    public void NameRejectsEmptyOrOverlongValues(string name)
    {
        var result = TargetValidator.ValidateAndNormalize(HttpDraft(name: name), 0);

        Assert.False(result.IsValid);
        Assert.Contains(nameof(TargetDraft.Name), result.Errors.Keys);
    }

    private static TargetDraft HttpDraft(
        string name = "Example",
        string address = "https://example.com",
        int pollIntervalSeconds = 10,
        int timeoutSeconds = 5) =>
        new(name, TargetType.Http, address, "ignored", pollIntervalSeconds, timeoutSeconds);
}
