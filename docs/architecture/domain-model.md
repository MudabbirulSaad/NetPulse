# Domain model

NetPulse models an HTTP or DNS endpoint as a `MonitorTarget`. UI input first becomes a
`TargetDraft`; the core validator either returns a normalized draft or field-level errors.
Only normalized drafts can become saved targets.

Each execution produces a typed `CheckResult`. HTTP and DNS details use distinct records,
so consumers do not depend on string keys or loosely typed metadata. A DNS result can carry
an independent ICMP signal, but that signal cannot change a successful DNS service result.

Health states have stable meanings:

- **Checking**: a check is active, or cancellation is being handled without inventing an outage.
- **Healthy**: an HTTP 2xx/3xx response or successful DNS A-record response.
- **Degraded**: an HTTP 4xx/5xx response or a DNS resolver response error.
- **Offline**: timeout, name-resolution, connection, TLS, or socket failure.
- **Error**: invalid target configuration or an unexpected application failure.

History is capped at the latest 100 results per target. Latency graphs show the latest 30
results and use gaps for failed checks. Minimum, average, and maximum latency include only
responses that reached the monitored service.
