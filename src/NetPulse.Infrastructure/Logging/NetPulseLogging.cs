using Serilog;
using Serilog.Events;
using Serilog.Core;
using NetPulse.Infrastructure.Storage;

namespace NetPulse.Infrastructure.Logging;

internal static class NetPulseLogging
{
    public static Logger Create(LocalStatePaths paths)
    {
        Directory.CreateDirectory(paths.LogsDirectory);
        var logPath = Path.Combine(paths.LogsDirectory, "netpulse-.log");

        return new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.WithProperty("Application", "NetPulse")
            .WriteTo.File(
                logPath,
                formatProvider: System.Globalization.CultureInfo.InvariantCulture,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,
                outputTemplate:
                    "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();
    }
}
