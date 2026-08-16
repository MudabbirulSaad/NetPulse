using NetPulse.Core.Models;
using NetPulse.Core.Monitoring;
using NetPulse.Core.Session;
using NetPulse.Core.Validation;
using NetPulse.Infrastructure.Monitoring;
using NetPulse.Infrastructure.Storage;

namespace NetPulse.Infrastructure.Session;

internal sealed class NetPulseSession : INetPulseSession
{
    private readonly Dictionary<Guid, TargetRuntime> _targets = [];
    private readonly Dictionary<TargetType, IProbe> _probes;
    private readonly ILocalStateStore _stateStore;
    private readonly TimeProvider _timeProvider;
    private readonly object _stateGate = new();
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private CancellationTokenSource? _runCancellation;
    private NetPulseState _currentState = NetPulseState.Empty;
    private bool _isInitialized;
    private bool _isRunning;
    private bool _isDisposed;

    public NetPulseSession(
        IEnumerable<IProbe> probes,
        ILocalStateStore stateStore,
        TimeProvider? timeProvider = null)
    {
        ArgumentNullException.ThrowIfNull(probes);
        ArgumentNullException.ThrowIfNull(stateStore);

        _probes = probes.ToDictionary(static probe => probe.TargetType);
        _stateStore = stateStore;
        _timeProvider = timeProvider ?? TimeProvider.System;

        if (!_probes.ContainsKey(TargetType.Http) || !_probes.ContainsKey(TargetType.Dns))
        {
            throw new ArgumentException(
                "HTTP and DNS probe adapters are required.",
                nameof(probes));
        }
    }

    public NetPulseState CurrentState => Volatile.Read(ref _currentState);

    public event EventHandler<NetPulseState>? StateChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await InitializeCoreAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await InitializeCoreAsync(cancellationToken).ConfigureAwait(false);

            if (_isRunning)
            {
                return;
            }

            _runCancellation = new CancellationTokenSource();
            _isRunning = true;

            TargetRuntime[] runtimes;
            lock (_stateGate)
            {
                runtimes = _targets.Values
                    .Where(static runtime => runtime.Target.IsEnabled)
                    .ToArray();
            }

            foreach (var runtime in runtimes)
            {
                StartLoop(runtime);
            }

            PublishState();
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_isDisposed)
        {
            return;
        }

        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (!_isRunning)
            {
                return;
            }

            _runCancellation?.Cancel();

            TargetRuntime[] runtimes;
            lock (_stateGate)
            {
                runtimes = _targets.Values.ToArray();
            }

            foreach (var runtime in runtimes)
            {
                await StopLoopAsync(runtime).ConfigureAwait(false);
            }

            _runCancellation?.Dispose();
            _runCancellation = null;
            _isRunning = false;
            PublishState();
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async Task<CheckResult> RunOnceAsync(
        TargetDraft target,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(target);

        var validation = TargetValidator.ValidateAndNormalize(
            target,
            CurrentState.Targets.Count,
            isEdit: true);
        if (!validation.IsValid)
        {
            return new CheckResult(
                Guid.Empty,
                _timeProvider.GetUtcNow(),
                TimeSpan.Zero,
                HealthState.Error,
                string.Join(" ", validation.Errors.Values.SelectMany(static value => value)),
                ProbeErrorCode.InvalidConfiguration);
        }

        var normalized = validation.Target!;
        TargetRuntime? existing;
        lock (_stateGate)
        {
            existing = _targets.Values.FirstOrDefault(runtime =>
                runtime.Target.Type == normalized.Type &&
                string.Equals(runtime.Target.Address, normalized.Address, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(runtime.Target.DnsResolver, normalized.DnsResolver, StringComparison.OrdinalIgnoreCase));
        }

        if (existing is not null)
        {
            return await ExecuteAsync(existing, persistResult: true, cancellationToken)
                .ConfigureAwait(false);
        }

        var transient = new TargetRuntime(new MonitorTarget(
            Guid.NewGuid(),
            normalized.Name,
            normalized.Type,
            normalized.Address,
            normalized.DnsResolver,
            normalized.PollIntervalSeconds,
            normalized.TimeoutSeconds,
            normalized.IsEnabled,
            _timeProvider.GetUtcNow()),
            Array.Empty<CheckResult>());

        try
        {
            return await ExecuteAsync(transient, persistResult: false, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            transient.Dispose();
        }
    }

    public async Task ApplyAsync(
        TargetChange change,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(change);
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await InitializeCoreAsync(cancellationToken).ConfigureAwait(false);

            switch (change)
            {
                case TargetChange.Add add:
                    await AddAsync(add.Draft, cancellationToken).ConfigureAwait(false);
                    break;
                case TargetChange.Update update:
                    await UpdateAsync(update, cancellationToken).ConfigureAwait(false);
                    break;
                case TargetChange.SetEnabled setEnabled:
                    await SetEnabledAsync(setEnabled, cancellationToken).ConfigureAwait(false);
                    break;
                case TargetChange.Delete delete:
                    await DeleteAsync(delete, cancellationToken).ConfigureAwait(false);
                    break;
            }

            await SaveStateAsync(cancellationToken).ConfigureAwait(false);
            PublishState();
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_isDisposed)
        {
            return;
        }

        await StopAsync().ConfigureAwait(false);

        lock (_stateGate)
        {
            foreach (var runtime in _targets.Values)
            {
                runtime.Dispose();
            }

            _targets.Clear();
        }

        _lifecycleGate.Dispose();
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    private async Task InitializeCoreAsync(CancellationToken cancellationToken)
    {
        if (_isInitialized)
        {
            return;
        }

        var stored = await _stateStore.LoadAsync(cancellationToken).ConfigureAwait(false)
            ?? StoredLocalState.Empty;

        lock (_stateGate)
        {
            foreach (var target in stored.Targets)
            {
                stored.History.TryGetValue(target.Id, out var history);
                _targets[target.Id] = new TargetRuntime(
                    target,
                    history ?? Array.Empty<CheckResult>());
            }
        }

        _isInitialized = true;
        PublishState();
    }

    private async Task AddAsync(TargetDraft draft, CancellationToken cancellationToken)
    {
        var validation = TargetValidator.ValidateAndNormalize(
            draft,
            _targets.Count,
            isEdit: false);
        EnsureValid(validation);
        var target = CreateTarget(validation.Target!);
        var runtime = new TargetRuntime(target, Array.Empty<CheckResult>());

        lock (_stateGate)
        {
            _targets.Add(target.Id, runtime);
        }

        if (_isRunning && target.IsEnabled)
        {
            StartLoop(runtime);
        }

        await Task.CompletedTask.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task UpdateAsync(
        TargetChange.Update update,
        CancellationToken cancellationToken)
    {
        var runtime = FindTarget(update.TargetId);
        var validation = TargetValidator.ValidateAndNormalize(
            update.Draft,
            _targets.Count,
            isEdit: true);
        EnsureValid(validation);

        await StopLoopAsync(runtime).ConfigureAwait(false);
        await runtime.CheckGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var draft = validation.Target!;
            runtime.Target = runtime.Target with
            {
                Name = draft.Name,
                Type = draft.Type,
                Address = draft.Address,
                DnsResolver = draft.DnsResolver,
                PollIntervalSeconds = draft.PollIntervalSeconds,
                TimeoutSeconds = draft.TimeoutSeconds,
                IsEnabled = draft.IsEnabled,
            };
        }
        finally
        {
            runtime.CheckGate.Release();
        }

        if (_isRunning && runtime.Target.IsEnabled)
        {
            StartLoop(runtime);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task SetEnabledAsync(
        TargetChange.SetEnabled change,
        CancellationToken cancellationToken)
    {
        var runtime = FindTarget(change.TargetId);
        await StopLoopAsync(runtime).ConfigureAwait(false);
        await runtime.CheckGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            runtime.Target = runtime.Target with { IsEnabled = change.IsEnabled };
        }
        finally
        {
            runtime.CheckGate.Release();
        }

        if (_isRunning && change.IsEnabled)
        {
            StartLoop(runtime);
        }

        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task DeleteAsync(
        TargetChange.Delete delete,
        CancellationToken cancellationToken)
    {
        var runtime = FindTarget(delete.TargetId);
        await StopLoopAsync(runtime).ConfigureAwait(false);
        await runtime.CheckGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            lock (_stateGate)
            {
                _targets.Remove(delete.TargetId);
            }
        }
        finally
        {
            runtime.CheckGate.Release();
        }

        runtime.Dispose();
        cancellationToken.ThrowIfCancellationRequested();
    }

    private async Task<CheckResult> ExecuteAsync(
        TargetRuntime runtime,
        bool persistResult,
        CancellationToken cancellationToken)
    {
        await runtime.CheckGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        var previousState = runtime.State;

        try
        {
            runtime.State = HealthState.Checking;
            if (persistResult)
            {
                PublishState();
            }

            CheckResult result;
            try
            {
                result = await _probes[runtime.Target.Type]
                    .CheckAsync(runtime.Target, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                runtime.State = previousState;
                if (persistResult)
                {
                    PublishState();
                }

                throw;
            }
            catch (Exception)
            {
                result = new CheckResult(
                    runtime.Target.Id,
                    _timeProvider.GetUtcNow(),
                    TimeSpan.Zero,
                    HealthState.Error,
                    "The target check failed unexpectedly.",
                    ProbeErrorCode.UnexpectedFailure);
            }

            runtime.State = result.State;

            if (persistResult)
            {
                runtime.History = HistoryPolicy.Append(runtime.History, result);
                await SaveStateAsync(cancellationToken).ConfigureAwait(false);
                PublishState();
            }

            return result;
        }
        finally
        {
            runtime.CheckGate.Release();
        }
    }

    private async Task RunLoopAsync(TargetRuntime runtime, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteAsync(runtime, persistResult: true, cancellationToken)
                    .ConfigureAwait(false);
                await Task.Delay(
                    TimeSpan.FromSeconds(runtime.Target.PollIntervalSeconds),
                    _timeProvider,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private void StartLoop(TargetRuntime runtime)
    {
        if (runtime.LoopTask is { IsCompleted: false } || _runCancellation is null)
        {
            return;
        }

        runtime.LoopCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            _runCancellation.Token);
        runtime.LoopTask = Task.Run(() =>
            RunLoopAsync(runtime, runtime.LoopCancellation.Token));
    }

    private static async Task StopLoopAsync(TargetRuntime runtime)
    {
        if (runtime.LoopCancellation is null)
        {
            return;
        }

        await runtime.LoopCancellation.CancelAsync().ConfigureAwait(false);

        if (runtime.LoopTask is not null)
        {
            try
            {
                await runtime.LoopTask.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
        }

        runtime.LoopCancellation.Dispose();
        runtime.LoopCancellation = null;
        runtime.LoopTask = null;
    }

    private async Task SaveStateAsync(CancellationToken cancellationToken)
    {
        StoredLocalState stored;
        lock (_stateGate)
        {
            stored = new StoredLocalState(
                _targets.Values.Select(static runtime => runtime.Target).ToArray(),
                _targets.ToDictionary(
                    static pair => pair.Key,
                    static pair => pair.Value.History));
        }

        await _stateStore.SaveAsync(stored, cancellationToken).ConfigureAwait(false);
    }

    private TargetRuntime FindTarget(Guid targetId)
    {
        lock (_stateGate)
        {
            return _targets.TryGetValue(targetId, out var runtime)
                ? runtime
                : throw new KeyNotFoundException($"Target {targetId} does not exist.");
        }
    }

    private MonitorTarget CreateTarget(TargetDraft draft) =>
        new(
            Guid.NewGuid(),
            draft.Name,
            draft.Type,
            draft.Address,
            draft.DnsResolver,
            draft.PollIntervalSeconds,
            draft.TimeoutSeconds,
            draft.IsEnabled,
            _timeProvider.GetUtcNow());

    private static void EnsureValid(TargetValidationResult result)
    {
        if (!result.IsValid)
        {
            throw new ArgumentException(
                string.Join(" ", result.Errors.Values.SelectMany(static value => value)));
        }
    }

    private void PublishState()
    {
        NetPulseState state;
        lock (_stateGate)
        {
            state = new NetPulseState(
                _isInitialized,
                _isRunning,
                _targets.Values
                    .OrderBy(static runtime => runtime.Target.CreatedAtUtc)
                    .Select(static runtime => new TargetSnapshot(
                        runtime.Target,
                        runtime.State,
                        runtime.History.Count == 0 ? null : runtime.History[^1],
                        runtime.History.ToArray()))
                    .ToArray());
            Volatile.Write(ref _currentState, state);
        }

        StateChanged?.Invoke(this, state);
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);
    }

    private sealed class TargetRuntime(
        MonitorTarget target,
        IReadOnlyList<CheckResult> history) : IDisposable
    {
        public MonitorTarget Target { get; set; } = target;

        public IReadOnlyList<CheckResult> History { get; set; } = history;

        public HealthState State { get; set; } = history.Count == 0
            ? HealthState.Checking
            : history[^1].State;

        public SemaphoreSlim CheckGate { get; } = new(1, 1);

        public CancellationTokenSource? LoopCancellation { get; set; }

        public Task? LoopTask { get; set; }

        public void Dispose()
        {
            LoopCancellation?.Dispose();
            CheckGate.Dispose();
        }
    }
}
