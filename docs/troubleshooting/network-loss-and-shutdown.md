# Network loss and shutdown

## Network loss

HTTP name-resolution, connection, TLS, and timeout failures become typed offline results. DNS
resolver transport failures follow the same rule, while a resolver response such as NXDOMAIN is
degraded. The scheduler isolates each target, so loss of one service or the whole network does
not stop other polling loops. Monitoring resumes naturally when later checks succeed.

## Application close

Closing the main window first cancels the session root token, awaits every active target loop,
disposes network clients and the local state store, and then permits the WPF window to close.
Caller cancellation restores the target's preceding state and does not add a false outage.

## Local logs

Daily files are written to `%LocalAppData%\NetPulse\logs\netpulse-YYYYMMDD.log` and the latest
14 files are retained. Events contain target IDs, target type, state, error code, duration, and
exception type where useful. Target names, URLs, resolver domains, query strings, and exception
messages are deliberately excluded so credentials or private endpoint details are not logged.
