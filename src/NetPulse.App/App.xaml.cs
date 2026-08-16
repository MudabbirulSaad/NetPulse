using System.Windows;
using System.ComponentModel;
using NetPulse.App.ViewModels;
using NetPulse.App.Services;
using NetPulse.Core.Session;
using NetPulse.Infrastructure.Session;

namespace NetPulse.App;

public partial class App : Application
{
    private INetPulseSession? _session;
    private bool _closeApproved;
    private bool _closeInProgress;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _session = NetPulseSessionFactory.CreateDefault();
        var viewModel = new DashboardViewModel(
            _session,
            SynchronizationContext.Current,
            new TargetDialogService(_session));
        var window = new MainWindow
        {
            DataContext = viewModel,
        };

        MainWindow = window;
        window.Closing += OnMainWindowClosing;
        window.Show();
        await viewModel.InitializeAsync();
    }

    private async void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        if (_closeApproved || sender is not Window window)
        {
            return;
        }

        e.Cancel = true;
        if (_closeInProgress)
        {
            return;
        }

        _closeInProgress = true;

        try
        {
            if (_session is not null)
            {
                await _session.DisposeAsync();
            }
        }
        finally
        {
            _session = null;
            _closeApproved = true;
            window.Closing -= OnMainWindowClosing;
            window.Close();
            Shutdown(0);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _session?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.OnExit(e);
    }
}
