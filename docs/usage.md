# Using NetPulse

## First run

NetPulse creates three examples only when no settings file exists: OpenReels (HTTP), Google DNS, and Cloudflare DNS. They are ordinary targets and can be edited, disabled, or deleted. A later run never recreates defaults after you remove them.

Recurring sampling starts after initialization. **Pause** cancels recurring loops without deleting state. **Resume** starts them again. **Run all now** performs one immediate check for every enabled target.

## Read the dashboard

The summary row is calculated from real target snapshots:

- **Total targets** counts configured targets.
- **Healthy** counts the latest healthy states.
- **Needs attention** counts degraded, offline, and error states.
- **Current average** averages available successful latency values.

Select a row in the target rail to see the latest message and duration, sample count, rolling min/average/max, and the latest 30-point latency graph. Failed samples appear as gaps. DNS targets also show the latest independent ICMP result.

## Add an HTTP target

1. Select **Add target** and choose `Http`.
2. Enter a descriptive name and an absolute `http://` or `https://` URL.
3. Select 5, 10, 30, or 60 seconds for the poll interval.
4. Enter a whole-number timeout from 1 to 30 seconds that is shorter than the interval.
5. Select **Test connection** if you want an immediate result, then save.

HTTP 2xx and 3xx responses are healthy. HTTP 4xx and 5xx responses are degraded. Transport failures are offline.

## Add a DNS target

1. Select **Add target** and choose `Dns`.
2. Enter a name, the domain to query, and a resolver IPv4 or IPv6 address.
3. Choose the polling interval and timeout.
4. Test the connection, then save.

NetPulse normalizes internationalized domains, sends an A-record query directly to the selected resolver, disables response caching, and does not retry automatically. A DNS response such as NXDOMAIN is degraded; a resolver timeout or socket failure is offline. ICMP is shown separately and does not change successful DNS health.

## Save an unreachable target

Saving runs a connection check. When valid configuration cannot currently be reached, NetPulse shows the result and asks whether to save anyway. This supports intentionally offline services without weakening validation. Invalid configuration cannot be saved.

## Manage targets

- **Test** performs one check under the same per-target lock used by recurring monitoring.
- **Edit** changes endpoint or timing details and restarts that target’s schedule.
- **Disable** cancels recurring checks for the target while retaining it and its history.
- **Enable** schedules a disabled target again.
- **Delete** asks for confirmation and removes the target and stored history.

The target limit is 25 and each target retains its latest 100 results.

## Files and logs

```text
%LocalAppData%\NetPulse\
├── settings.json
├── history.json
└── logs\netpulse-YYYYMMDD.log
```

Files are replaced atomically. If settings or history contains invalid JSON, NetPulse renames it with a timestamp, restores a safe default, and presents a readable warning. Logs roll daily and retain 14 files.
