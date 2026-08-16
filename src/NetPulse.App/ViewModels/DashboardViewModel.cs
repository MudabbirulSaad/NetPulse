using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetPulse.Core.Models;
using NetPulse.Core.Session;

namespace NetPulse.App.ViewModels;

public sealed class DashboardViewModel : ObservableObject
{
    private readonly INetPulseSession _session;
    private readonly SynchronizationContext? _uiContext;
    private TargetRowViewModel? _selectedTarget;
    private bool _isRunning;
    private bool _isBusy;
    private int _totalTargets;
    private int _healthyTargets;
    private int _attentionTargets;
    private string _averageLatency = "—";
    private string? _warning;
    private string? _errorMessage;

    public DashboardViewModel(
        INetPulseSession session,
        SynchronizationContext? uiContext = null)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _uiContext = uiContext;
        _session.StateChanged += OnStateChanged;
        ToggleMonitoringCommand = new AsyncRelayCommand(ToggleMonitoringAsync, CanUseCommands);
        RunAllNowCommand = new AsyncRelayCommand(RunAllNowAsync, CanRunAll);
    }

    public ObservableCollection<TargetRowViewModel> Targets { get; } = [];

    public IAsyncRelayCommand ToggleMonitoringCommand { get; }

    public IAsyncRelayCommand RunAllNowCommand { get; }

    public TargetRowViewModel? SelectedTarget
    {
        get => _selectedTarget;
        set => SetProperty(ref _selectedTarget, value);
    }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(MonitoringStatus));
                OnPropertyChanged(nameof(ToggleMonitoringLabel));
            }
        }
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                NotifyCommands();
            }
        }
    }

    public int TotalTargets
    {
        get => _totalTargets;
        private set => SetProperty(ref _totalTargets, value);
    }

    public int HealthyTargets
    {
        get => _healthyTargets;
        private set => SetProperty(ref _healthyTargets, value);
    }

    public int AttentionTargets
    {
        get => _attentionTargets;
        private set => SetProperty(ref _attentionTargets, value);
    }

    public string AverageLatency
    {
        get => _averageLatency;
        private set => SetProperty(ref _averageLatency, value);
    }

    public string? Warning
    {
        get => _warning;
        private set => SetProperty(ref _warning, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string MonitoringStatus => IsRunning ? "LIVE SAMPLING" : "MONITORING PAUSED";

    public string ToggleMonitoringLabel => IsRunning ? "PAUSE" : "RESUME";

    public async Task InitializeAsync()
    {
        await RunBusyAsync(async () =>
        {
            await _session.InitializeAsync();
            ApplyState(_session.CurrentState);
            await _session.StartAsync();
        });
    }

    private async Task ToggleMonitoringAsync()
    {
        await RunBusyAsync(async () =>
        {
            if (_session.CurrentState.IsRunning)
            {
                await _session.StopAsync();
            }
            else
            {
                await _session.StartAsync();
            }
        });
    }

    private async Task RunAllNowAsync()
    {
        await RunBusyAsync(async () =>
        {
            foreach (var target in Targets.Where(static target => target.IsEnabled).ToArray())
            {
                await _session.RunOnceAsync(target.ToDraft());
            }
        });
    }

    private async Task RunBusyAsync(Func<Task> operation)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            await operation();
        }
        catch (Exception exception)
        {
            ErrorMessage = $"NetPulse could not complete the action: {exception.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void OnStateChanged(object? sender, NetPulseState state)
    {
        if (_uiContext is null || ReferenceEquals(SynchronizationContext.Current, _uiContext))
        {
            ApplyState(state);
            return;
        }

        _uiContext.Post(static value =>
        {
            var update = (StateUpdate)value!;
            update.ViewModel.ApplyState(update.State);
        }, new StateUpdate(this, state));
    }

    private void ApplyState(NetPulseState state)
    {
        var selectedId = SelectedTarget?.Id;
        Targets.Clear();

        foreach (var target in state.Targets)
        {
            Targets.Add(new TargetRowViewModel(target));
        }

        SelectedTarget = Targets.FirstOrDefault(target => target.Id == selectedId)
            ?? Targets.FirstOrDefault();
        IsRunning = state.IsRunning;
        TotalTargets = Targets.Count;
        HealthyTargets = Targets.Count(static target => target.State == HealthState.Healthy);
        AttentionTargets = Targets.Count(static target =>
            target.State is HealthState.Degraded or HealthState.Offline or HealthState.Error);
        var measured = state.Targets
            .Select(static target => target.LatestResult)
            .Where(static result => result is { HasLatency: true })
            .Select(static result => result!.Duration.TotalMilliseconds)
            .ToArray();
        AverageLatency = measured.Length == 0
            ? "—"
            : $"{measured.Average():0} ms";
        Warning = state.Warning;
        NotifyCommands();
    }

    private bool CanUseCommands() => !IsBusy;

    private bool CanRunAll() => !IsBusy && Targets.Any(static target => target.IsEnabled);

    private void NotifyCommands()
    {
        ToggleMonitoringCommand.NotifyCanExecuteChanged();
        RunAllNowCommand.NotifyCanExecuteChanged();
    }

    private sealed record StateUpdate(DashboardViewModel ViewModel, NetPulseState State);
}
