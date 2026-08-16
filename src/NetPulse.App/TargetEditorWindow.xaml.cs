using System.Windows;
using NetPulse.App.ViewModels;
using NetPulse.Core.Models;

namespace NetPulse.App;

public partial class TargetEditorWindow : Window
{
    public TargetEditorWindow()
    {
        InitializeComponent();
    }

    public TargetDraft? ResultDraft { get; private set; }

    private async void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (DataContext is not TargetEditorViewModel viewModel)
        {
            return;
        }

        SaveButton.IsEnabled = false;

        try
        {
            var result = await viewModel.PrepareSaveAsync();
            if (result is null)
            {
                return;
            }

            if (result.RequiresUnreachableWarning)
            {
                var choice = MessageBox.Show(
                    this,
                    "NetPulse could not reach this target now. Save it anyway so you can diagnose it later?",
                    "Target is unreachable",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning,
                    MessageBoxResult.No);
                if (choice != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            ResultDraft = result.Draft;
            DialogResult = true;
        }
        finally
        {
            SaveButton.IsEnabled = true;
        }
    }
}
