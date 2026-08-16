using NetPulse.Core.Models;
using NetPulse.Core.Validation;

namespace NetPulse.Core.Monitoring;

public static class HistoryPolicy
{
    public static IReadOnlyList<CheckResult> Append(
        IEnumerable<CheckResult> existing,
        CheckResult result)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(result);

        return existing
            .Append(result)
            .TakeLast(TargetValidator.MaximumHistoryResults)
            .ToArray();
    }
}
