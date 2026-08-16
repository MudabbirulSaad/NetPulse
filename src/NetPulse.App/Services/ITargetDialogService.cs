using NetPulse.App.ViewModels;
using NetPulse.Core.Models;

namespace NetPulse.App.Services;

public interface ITargetDialogService
{
    TargetDraft? ShowEditor(TargetRowViewModel? existingTarget, int currentTargetCount);

    bool ConfirmDelete(TargetRowViewModel target);
}
