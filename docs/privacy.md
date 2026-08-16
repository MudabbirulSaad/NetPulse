# Privacy Statement

NetPulse is a local desktop utility. It has no telemetry SDK, analytics endpoint, account system, advertising, remote configuration, or cloud data store.

## Network activity

The application sends checks only for enabled targets configured in its local target list:

- HTTP or HTTPS requests to the configured URL, with the user agent `NetPulse/1.0`.
- DNS A-record queries to the configured resolver IP address for the configured domain.
- An independent ICMP echo attempt to the resolver used by a DNS target.

Remote services, DNS operators, network administrators, and Internet providers may observe those requests under their own policies. NetPulse does not proxy them through a project-operated service.

## Local data

Settings, up to 100 history records per target, corruption backups, and rolling logs are stored under `%LocalAppData%\NetPulse` for the current user. They are not encrypted by NetPulse and inherit Windows file permissions. Anyone with access to that Windows profile may be able to read them.

Logs contain target identifiers, target type, state, error code, duration, and exception type when useful. They deliberately omit target names, endpoint URLs, resolver domains, query strings, credentials, and exception messages.

## Retention and deletion

History is trimmed automatically to 100 results per target. Logs retain 14 daily files. MSI removal leaves the local folder untouched so reinstall and upgrade do not destroy user state. To erase all NetPulse data, uninstall the app and manually delete `%LocalAppData%\NetPulse`.

## Public evidence

Public screenshots and issue reports must contain only non-sensitive demonstration targets. Assignment evidence, student details, private endpoints, and raw logs stay outside the public repository.
