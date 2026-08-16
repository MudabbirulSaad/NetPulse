using System.Globalization;
using System.Net;
using NetPulse.Core.Models;

namespace NetPulse.Core.Validation;

public static class TargetValidator
{
    public const int MaximumTargets = 25;
    public const int MaximumNameLength = 60;
    public const int MaximumHistoryResults = 100;
    public const int MaximumGraphResults = 30;

    private static readonly HashSet<int> AllowedPollIntervals = [5, 10, 30, 60];

    public static TargetValidationResult ValidateAndNormalize(
        TargetDraft draft,
        int currentTargetCount,
        bool isEdit = false)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var errors = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        var name = draft.Name.Trim();
        var address = draft.Address.Trim();
        var resolver = string.IsNullOrWhiteSpace(draft.DnsResolver)
            ? null
            : draft.DnsResolver.Trim();

        AddNameErrors(name, errors);
        AddCountErrors(currentTargetCount, isEdit, errors);
        AddTimingErrors(draft.PollIntervalSeconds, draft.TimeoutSeconds, errors);

        if (draft.Type == TargetType.Http)
        {
            address = NormalizeHttpAddress(address, errors);
            resolver = null;
        }
        else
        {
            address = NormalizeDomain(address, errors);
            AddResolverErrors(resolver, errors);
        }

        if (errors.Count > 0)
        {
            return new TargetValidationResult(null, errors);
        }

        return new TargetValidationResult(
            draft with
            {
                Name = name,
                Address = address,
                DnsResolver = resolver,
            },
            errors);
    }

    private static void AddNameErrors(
        string name,
        Dictionary<string, IReadOnlyList<string>> errors)
    {
        if (name.Length is < 1 or > MaximumNameLength)
        {
            errors[nameof(TargetDraft.Name)] =
                [$"Name must contain between 1 and {MaximumNameLength} characters."];
        }
    }

    private static void AddCountErrors(
        int currentTargetCount,
        bool isEdit,
        Dictionary<string, IReadOnlyList<string>> errors)
    {
        if (!isEdit && currentTargetCount >= MaximumTargets)
        {
            errors["Targets"] = [$"A maximum of {MaximumTargets} targets is supported."];
        }
    }

    private static void AddTimingErrors(
        int pollIntervalSeconds,
        int timeoutSeconds,
        Dictionary<string, IReadOnlyList<string>> errors)
    {
        if (!AllowedPollIntervals.Contains(pollIntervalSeconds))
        {
            errors[nameof(TargetDraft.PollIntervalSeconds)] =
                ["Poll interval must be 5, 10, 30, or 60 seconds."];
        }

        if (timeoutSeconds is < 1 or > 30 || timeoutSeconds >= pollIntervalSeconds)
        {
            errors[nameof(TargetDraft.TimeoutSeconds)] =
                ["Timeout must be between 1 and 30 seconds and shorter than the poll interval."];
        }
    }

    private static string NormalizeHttpAddress(
        string address,
        Dictionary<string, IReadOnlyList<string>> errors)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
            string.IsNullOrWhiteSpace(uri.Host))
        {
            errors[nameof(TargetDraft.Address)] =
                ["Address must be an absolute HTTP or HTTPS URL."];
            return address;
        }

        return uri.AbsoluteUri;
    }

    private static string NormalizeDomain(
        string address,
        Dictionary<string, IReadOnlyList<string>> errors)
    {
        var candidate = address.TrimEnd('.');

        try
        {
            var asciiDomain = new IdnMapping().GetAscii(candidate).ToLowerInvariant();
            var labelsAreValid = asciiDomain.Length <= 253 &&
                asciiDomain.Split('.').All(static label =>
                    label.Length is >= 1 and <= 63 &&
                    label[0] != '-' &&
                    label[^1] != '-');

            if (!asciiDomain.Contains('.', StringComparison.Ordinal) ||
                !labelsAreValid ||
                Uri.CheckHostName(asciiDomain) != UriHostNameType.Dns ||
                IPAddress.TryParse(asciiDomain, out _))
            {
                throw new FormatException("Domain is not valid.");
            }

            return asciiDomain;
        }
        catch (ArgumentException)
        {
            errors[nameof(TargetDraft.Address)] = ["Address must be a valid DNS domain."];
            return address;
        }
        catch (FormatException)
        {
            errors[nameof(TargetDraft.Address)] = ["Address must be a valid DNS domain."];
            return address;
        }
    }

    private static void AddResolverErrors(
        string? resolver,
        Dictionary<string, IReadOnlyList<string>> errors)
    {
        if (resolver is null || !IPAddress.TryParse(resolver, out _))
        {
            errors[nameof(TargetDraft.DnsResolver)] =
                ["DNS resolver must be a valid IPv4 or IPv6 address."];
        }
    }
}
