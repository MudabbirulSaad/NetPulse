namespace NetPulse.Infrastructure.Storage;

internal sealed record LocalStatePaths(
    string RootDirectory,
    string SettingsFile,
    string HistoryFile,
    string LogsDirectory)
{
    public static LocalStatePaths CreateDefault()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NetPulse");
        return FromRoot(root);
    }

    public static LocalStatePaths FromRoot(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        var fullRoot = Path.GetFullPath(rootDirectory);

        return new LocalStatePaths(
            fullRoot,
            Path.Combine(fullRoot, "settings.json"),
            Path.Combine(fullRoot, "history.json"),
            Path.Combine(fullRoot, "logs"));
    }
}
