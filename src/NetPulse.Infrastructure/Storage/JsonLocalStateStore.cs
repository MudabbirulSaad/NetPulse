using System.Text.Json;
using System.Text.Json.Serialization;
using NetPulse.Core.Models;
using NetPulse.Core.Validation;

namespace NetPulse.Infrastructure.Storage;

internal sealed class JsonLocalStateStore(
    LocalStatePaths paths,
    TimeProvider? timeProvider = null) : ILocalStateStore, IDisposable
{
    private const int CurrentFormatVersion = 1;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private readonly SemaphoreSlim _ioGate = new(1, 1);

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    public async Task<StoredLocalState?> LoadAsync(CancellationToken cancellationToken)
    {
        await _ioGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            Directory.CreateDirectory(paths.RootDirectory);
            var settingsExists = File.Exists(paths.SettingsFile);
            var historyExists = File.Exists(paths.HistoryFile);

            if (!settingsExists && !historyExists)
            {
                return null;
            }

            var warnings = new List<string>();
            var shouldSeedDefaults = !settingsExists;
            SettingsDocument settings;

            try
            {
                settings = settingsExists
                    ? await ReadAsync<SettingsDocument>(paths.SettingsFile, cancellationToken)
                        .ConfigureAwait(false)
                    : new SettingsDocument(CurrentFormatVersion, Array.Empty<MonitorTarget>());
            }
            catch (JsonException)
            {
                var renamed = PreserveCorruptedFile(paths.SettingsFile);
                warnings.Add(
                    $"settings.json could not be read and was preserved as {Path.GetFileName(renamed)}. Default targets were restored.");
                settings = new SettingsDocument(
                    CurrentFormatVersion,
                    Array.Empty<MonitorTarget>());
                shouldSeedDefaults = true;
            }
            catch (NotSupportedException)
            {
                var renamed = PreserveCorruptedFile(paths.SettingsFile);
                warnings.Add(
                    $"settings.json used unsupported data and was preserved as {Path.GetFileName(renamed)}. Default targets were restored.");
                settings = new SettingsDocument(
                    CurrentFormatVersion,
                    Array.Empty<MonitorTarget>());
                shouldSeedDefaults = true;
            }

            HistoryDocument history;
            try
            {
                history = historyExists
                    ? await ReadAsync<HistoryDocument>(paths.HistoryFile, cancellationToken)
                        .ConfigureAwait(false)
                    : new HistoryDocument(CurrentFormatVersion, Array.Empty<TargetHistoryDocument>());
            }
            catch (JsonException)
            {
                var renamed = PreserveCorruptedFile(paths.HistoryFile);
                warnings.Add(
                    $"history.json could not be read and was preserved as {Path.GetFileName(renamed)}. Monitoring history was reset.");
                history = new HistoryDocument(
                    CurrentFormatVersion,
                    Array.Empty<TargetHistoryDocument>());
            }
            catch (NotSupportedException)
            {
                var renamed = PreserveCorruptedFile(paths.HistoryFile);
                warnings.Add(
                    $"history.json used unsupported data and was preserved as {Path.GetFileName(renamed)}. Monitoring history was reset.");
                history = new HistoryDocument(
                    CurrentFormatVersion,
                    Array.Empty<TargetHistoryDocument>());
            }

            var historyByTarget = history.Targets.ToDictionary(
                static item => item.TargetId,
                static item => (IReadOnlyList<CheckResult>)item.Results
                    .TakeLast(TargetValidator.MaximumHistoryResults)
                    .ToArray());

            return new StoredLocalState(
                settings.Targets,
                historyByTarget,
                warnings.Count == 0 ? null : string.Join(" ", warnings),
                shouldSeedDefaults);
        }
        finally
        {
            _ioGate.Release();
        }
    }

    public async Task SaveAsync(
        StoredLocalState state,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(state);
        await _ioGate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            Directory.CreateDirectory(paths.RootDirectory);
            var settings = new SettingsDocument(
                CurrentFormatVersion,
                state.Targets.ToArray());
            var history = new HistoryDocument(
                CurrentFormatVersion,
                state.History
                    .Select(static item => new TargetHistoryDocument(
                        item.Key,
                        item.Value
                            .TakeLast(TargetValidator.MaximumHistoryResults)
                            .ToArray()))
                    .ToArray());

            await WriteAtomicallyAsync(
                paths.SettingsFile,
                settings,
                cancellationToken).ConfigureAwait(false);
            await WriteAtomicallyAsync(
                paths.HistoryFile,
                history,
                cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _ioGate.Release();
        }
    }

    public void Dispose()
    {
        _ioGate.Dispose();
    }

    private static async Task<T> ReadAsync<T>(
        string path,
        CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            new FileStreamOptions
            {
                Mode = FileMode.Open,
                Access = FileAccess.Read,
                Share = FileShare.Read,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            });

        return await JsonSerializer.DeserializeAsync<T>(
            stream,
            SerializerOptions,
            cancellationToken).ConfigureAwait(false)
            ?? throw new JsonException($"{Path.GetFileName(path)} contained no JSON value.");
    }

    private static async Task WriteAtomicallyAsync<T>(
        string destination,
        T value,
        CancellationToken cancellationToken)
    {
        var temporary = Path.Combine(
            Path.GetDirectoryName(destination)!,
            $".{Path.GetFileName(destination)}.{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var stream = new FileStream(
                temporary,
                new FileStreamOptions
                {
                    Mode = FileMode.CreateNew,
                    Access = FileAccess.Write,
                    Share = FileShare.None,
                    Options = FileOptions.Asynchronous | FileOptions.WriteThrough,
                }))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    value,
                    SerializerOptions,
                    cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private string PreserveCorruptedFile(string path)
    {
        var timestamp = _timeProvider.GetUtcNow().ToString(
            "yyyyMMdd-HHmmssfff",
            System.Globalization.CultureInfo.InvariantCulture);
        var renamed = Path.Combine(
            Path.GetDirectoryName(path)!,
            $"{Path.GetFileNameWithoutExtension(path)}.corrupt-{timestamp}{Path.GetExtension(path)}");
        File.Move(path, renamed, overwrite: true);
        return renamed;
    }

    private sealed record SettingsDocument(
        int FormatVersion,
        IReadOnlyList<MonitorTarget> Targets);

    private sealed record HistoryDocument(
        int FormatVersion,
        IReadOnlyList<TargetHistoryDocument> Targets);

    private sealed record TargetHistoryDocument(
        Guid TargetId,
        IReadOnlyList<CheckResult> Results);
}
