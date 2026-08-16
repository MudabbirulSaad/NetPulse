# HTTP probe

The HTTP adapter owns one long-lived `HttpClient` in production. Requests use the
`NetPulse/1.0` user agent and complete after response headers arrive, avoiding unnecessary
response-body downloads. The target timeout and application cancellation token are linked,
but caller cancellation is propagated rather than reported as a false outage.

Status mapping is deliberately small:

- 2xx and 3xx responses are healthy.
- 4xx and 5xx responses are degraded because the service responded.
- DNS, connection, TLS, and timeout failures are offline.
- Invalid configuration and unexpected application failures are errors.

Ordinary tests use an in-memory HTTP message handler. Real-network checks remain opt-in so
CI results do not depend on an external service.
