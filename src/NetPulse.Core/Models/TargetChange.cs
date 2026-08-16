namespace NetPulse.Core.Models;

public abstract record TargetChange
{
    private TargetChange()
    {
    }

    public sealed record Add(TargetDraft Draft) : TargetChange;

    public sealed record Update(Guid TargetId, TargetDraft Draft) : TargetChange;

    public sealed record SetEnabled(Guid TargetId, bool IsEnabled) : TargetChange;

    public sealed record Delete(Guid TargetId) : TargetChange;
}
