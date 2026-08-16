# ADR-002: App-facing monitoring interface

## Status

Accepted

## Decision

Expose monitoring to WPF through one `INetPulseSession` interface. Keep probe and persistence seams internal to the core/infrastructure implementation and supply in-memory adapters in tests.

## Consequences

- View models do not coordinate timers, cancellation, files, or networking.
- Tests exercise the same session interface used by the UI.
- New probe implementations do not expand the WPF-facing interface.
