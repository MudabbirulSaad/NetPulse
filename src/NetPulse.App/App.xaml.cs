using System.Windows;
using NetPulse.App.ViewModels;
using NetPulse.Core.Session;
using NetPulse.Infrastructure.Session;

namespace NetPulse.App;

public partial class App : Application
{
    private INetPulseSession? _session;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _session = NetPulseSessionFactory.CreateDefault();
        var viewModel = new DashboardViewModel(
            _session,
            SynchronizationContext.Current);
        var window = new MainWindow
        {
            DataContext = viewModel,
        };

        MainWindow = window;
        window.Show();
        await viewModel.InitializeAsync();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _session?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        base.OnExit(e);
    }
}
