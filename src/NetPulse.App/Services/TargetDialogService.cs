using System.Windows;
using NetPulse.App.ViewModels;
using NetPulse.Core.Models;
using NetPulse.Core.Session;

namespace NetPulse.App.Services;

public sealed class TargetDialogService(INetPulseSession session) : ITargetDialogService
{
    public TargetDraft? ShowEditor(
        TargetRowViewModel? existingTarget,
        int currentTargetCount)
    {
        var viewModel = new TargetEditorViewModel(
            session,
            existingTarget,
            currentTargetCount);
        var dialog = new TargetEditorWindow
        {
            Owner = Application.Current.MainWindow,
            DataContext = viewModel,
        };

        return dialog.ShowDialog() == true
            ? dialog.ResultDraft
            : null;
    }

    public bool ConfirmDelete(TargetRowViewModel target) =>
        MessageBox.Show(
            Application.Current.MainWindow,
            $"Delete ‘{target.Name}’ and its local monitoring history?",
            "Delete target",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;
}
