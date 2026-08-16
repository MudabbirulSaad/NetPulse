namespace NetPulse.Core.Models;

public sealed record TargetValidationResult(
    TargetDraft? Target,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Errors)
{
    public bool IsValid => Target is not null && Errors.Count == 0;
}
