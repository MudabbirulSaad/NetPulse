using NetPulse.Core.Session;
using NetPulse.Infrastructure.Monitoring;
using NetPulse.Infrastructure.Logging;
using NetPulse.Infrastructure.Storage;

namespace NetPulse.Infrastructure.Session;

public static class NetPulseSessionFactory
{
    public static INetPulseSession CreateDefault()
    {
        var paths = LocalStatePaths.CreateDefault();
        var logger = NetPulseLogging.Create(paths);
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = System.Net.DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
        };
        var httpClient = new HttpClient(handler, disposeHandler: true);
        var probes = new IProbe[]
        {
            new HttpProbe(httpClient, ownsClient: true),
            new DnsProbe(new DnsClientLookup(), new IcmpProbe()),
        };
        var store = new JsonLocalStateStore(paths);

        return new NetPulseSession(
            probes,
            store,
            logger: logger,
            ownsLogger: true);
    }
}
