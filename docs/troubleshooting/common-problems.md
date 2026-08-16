# Common Problems

## The app asks for .NET Desktop Runtime

Install Microsoft .NET Desktop Runtime 10 x64. The base .NET Runtime or ASP.NET Core Runtime alone does not include WPF. See [the prerequisite guide](../runtime-prerequisite.md).

## A URL is rejected

Use an absolute URL beginning with `http://` or `https://`. Relative paths and other schemes are not accepted. A timeout must be shorter than the selected poll interval.

## A DNS target is rejected

Enter a domain in the endpoint field and a literal IPv4 or IPv6 address in the resolver field. Resolver host names are intentionally not accepted because the target must test a specific resolver.

## HTTP is degraded rather than offline

An HTTP server that returns 4xx or 5xx is reachable but unhealthy, so NetPulse marks it degraded. Name resolution, connection, TLS, and timeout failures are offline.

## DNS is healthy but ICMP failed

This is expected on networks or resolvers that block echo requests. ICMP is informational and never overrides a successful DNS query.

## Measurements differ between checks

Latency includes local scheduling, the network path, and the remote service. NetPulse reports the actual check duration and does not smooth individual samples. Use the rolling min/average/max and graph to understand a trend.

## Monitoring paused during close

Closing cancels active checks before the WPF dispatcher exits. Cancellation is not recorded as an outage. A slow close lasting roughly the remaining cancellation time should be captured with the local log and investigated.

## Settings or history was reset

If JSON cannot be parsed, NetPulse renames the corrupt file with a timestamp and restores safe state. Look in `%LocalAppData%\NetPulse` for the backup and in the latest log for the technical category. Do not paste private endpoint data into a public issue.

## The MSI reports insufficient privileges

The package installs under Program Files and is per-machine. Run it from an administrator account or use the provided install script in an elevated PowerShell session. Windows Installer logs are written beneath `artifacts` when the script is used from a source checkout.

## Where to collect diagnostic context

Record the NetPulse version, Windows build, .NET Desktop Runtime version, target type, displayed health/error code, and the timestamp. Logs are under `%LocalAppData%\NetPulse\logs`. Inspect them before sharing; the application avoids endpoint values, but public reports should still be sanitized.
