# ADR-003: Local data policy

## Status

Accepted

## Decision

Store targets, rolling history, and logs under `%LocalAppData%\NetPulse`. Preserve this user data during MSI uninstall and document manual removal.

## Consequences

- Application upgrades and reinstallations keep user targets.
- Corrupt JSON can be quarantined without losing executable integrity.
- Uninstall removes Program Files content and shortcuts but not user-created state.
