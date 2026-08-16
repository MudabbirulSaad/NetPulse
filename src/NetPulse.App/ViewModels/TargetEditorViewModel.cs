using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NetPulse.Core.Models;
using NetPulse.Core.Session;
using NetPulse.Core.Validation;

namespace NetPulse.App.ViewModels;

public sealed class TargetEditorViewModel : ObservableObject
{
    private readonly INetPulseSession _session;
    private readonly int _currentTargetCount;
    private readonly bool _isEdit;
    private string _name = string.Empty;
    private TargetType _targetType = TargetType.Http;
    private string _address = string.Empty;
    private string _dnsResolver = "1.1.1.1";
    private int _pollIntervalSeconds = 10;
    private string _timeoutText = "5";
    private bool _isEnabled = true;
    private bool _isBusy;
    private string? _nameError;
    private string? _addressError;
    private string? _resolverError;
    private string? _timingError;
    private string? _generalError;
    private string _testStatus = "No connection test has run.";
    private HealthState _testState = HealthState.Checking;

    public TargetEditorViewModel(
        INetPulseSession session,
        TargetRowViewModel? existingTarget,
        int currentTargetCount)
    {
        ArgumentNullException.ThrowIfNull(session);
        _session = session;
        _currentTargetCount = currentTargetCount;
        _isEdit = existingTarget is not null;

        if (existingTarget is not null)
        {
            var target = existingTarget.Snapshot.Target;
            _name = target.Name;
            _targetType = target.Type;
            _address = target.Address;
            _dnsResolver = target.DnsResolver ?? "1.1.1.1";
            _pollIntervalSeconds = target.PollIntervalSeconds;
            _timeoutText = target.TimeoutSeconds.ToString(
                System.Globalization.CultureInfo.InvariantCulture);
            _isEnabled = target.IsEnabled;
        }

        TestConnectionCommand = new AsyncRelayCommand(
            async () => await TestConnectionAsync(),
            () => !IsBusy);
    }

    public IReadOnlyList<TargetType> TargetTypes { get; } =
        [TargetType.Http, TargetType.Dns];

    public IReadOnlyList<int> PollIntervals { get; } = [5, 10, 30, 60];

    public IAsyncRelayCommand TestConnectionCommand { get; }

    public CheckResult? LastTestResult { get; private set; }

    public string DialogTitle => _isEdit ? "EDIT TARGET" : "ADD TARGET";

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public TargetType TargetType
    {
        get => _targetType;
        set
        {
            if (SetProperty(ref _targetType, value))
            {
                OnPropertyChanged(nameof(IsDns));
                OnPropertyChanged(nameof(AddressLabel));
                OnPropertyChanged(nameof(AddressHint));
            }
        }
    }

    public string Address
    {
        get => _address;
        set => SetProperty(ref _address, value);
    }

    public string DnsResolver
    {
        get => _dnsResolver;
        set => SetProperty(ref _dnsResolver, value);
    }

    public int PollIntervalSeconds
    {
        get => _pollIntervalSeconds;
        set => SetProperty(ref _pollIntervalSeconds, value);
    }

    public string TimeoutText
    {
        get => _timeoutText;
        set => SetProperty(ref _timeoutText, value);
    }

    public bool IsEnabled
    {
        get => _isEnabled;
        set => SetProperty(ref _isEnabled, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                TestConnectionCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsDns => TargetType == TargetType.Dns;

    public string AddressLabel => IsDns ? "DOMAIN NAME" : "HTTP OR HTTPS URL";

    public string AddressHint => IsDns
        ? "example.com"
        : "https://example.com/health";

    public string? NameError
    {
        get => _nameError;
        private set => SetProperty(ref _nameError, value);
    }

    public string? AddressError
    {
        get => _addressError;
        private set => SetProperty(ref _addressError, value);
    }

    public string? ResolverError
    {
        get => _resolverError;
        private set => SetProperty(ref _resolverError, value);
    }

    public string? TimingError
    {
        get => _timingError;
        private set => SetProperty(ref _timingError, value);
    }

    public string? GeneralError
    {
        get => _generalError;
        private set => SetProperty(ref _generalError, value);
    }

    public string TestStatus
    {
        get => _testStatus;
        private set => SetProperty(ref _testStatus, value);
    }

    public HealthState TestState
    {
        get => _testState;
        private set => SetProperty(ref _testState, value);
    }

    public TargetDraft? BuildValidatedDraft()
    {
        ClearErrors();

        if (!int.TryParse(
                TimeoutText,
                System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture,
                out var timeoutSeconds))
        {
            TimingError = "Timeout must be a whole number of seconds.";
            return null;
        }

        var draft = new TargetDraft(
            Name,
            TargetType,
            Address,
            IsDns ? DnsResolver : null,
            PollIntervalSeconds,
            timeoutSeconds,
            IsEnabled);
        var validation = TargetValidator.ValidateAndNormalize(
            draft,
            _currentTargetCount,
            _isEdit);

        if (!validation.IsValid)
        {
            ApplyErrors(validation.Errors);
            return null;
        }

        return validation.Target;
    }

    public async Task<CheckResult?> TestConnectionAsync()
    {
        var draft = BuildValidatedDraft();
        if (draft is null)
        {
            TestState = HealthState.Error;
            TestStatus = "Correct the highlighted fields before testing.";
            return null;
        }

        IsBusy = true;
        TestState = HealthState.Checking;
        TestStatus = "Checking the target now…";

        try
        {
            LastTestResult = await _session.RunOnceAsync(draft);
            TestState = LastTestResult.State;
            TestStatus = $"{LastTestResult.State.ToString().ToUpperInvariant()} · {LastTestResult.Message}";
            return LastTestResult;
        }
        catch (Exception exception)
        {
            GeneralError = $"Connection test failed: {exception.Message}";
            TestState = HealthState.Error;
            TestStatus = "The connection test could not be completed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task<TargetEditorResult?> PrepareSaveAsync()
    {
        var draft = BuildValidatedDraft();
        if (draft is null)
        {
            return null;
        }

        var result = await TestConnectionAsync();
        return result is null
            ? null
            : new TargetEditorResult(draft, result);
    }

    private void ApplyErrors(IReadOnlyDictionary<string, IReadOnlyList<string>> errors)
    {
        NameError = First(errors, nameof(TargetDraft.Name));
        AddressError = First(errors, nameof(TargetDraft.Address));
        ResolverError = First(errors, nameof(TargetDraft.DnsResolver));
        TimingError = string.Join(
            " ",
            new[]
            {
                First(errors, nameof(TargetDraft.PollIntervalSeconds)),
                First(errors, nameof(TargetDraft.TimeoutSeconds)),
            }.Where(static value => value is not null));
        GeneralError = First(errors, "Targets");
    }

    private void ClearErrors()
    {
        NameError = null;
        AddressError = null;
        ResolverError = null;
        TimingError = null;
        GeneralError = null;
    }

    private static string? First(
        IReadOnlyDictionary<string, IReadOnlyList<string>> errors,
        string key) => errors.TryGetValue(key, out var messages) && messages.Count > 0
        ? messages[0]
        : null;
}

public sealed record TargetEditorResult(TargetDraft Draft, CheckResult TestResult)
{
    public bool RequiresUnreachableWarning =>
        TestResult.State is HealthState.Offline or HealthState.Error;
}
