# Local persistence

NetPulse keeps user-owned data under `%LocalAppData%\NetPulse`:

```text
settings.json
history.json
logs\
```

Settings and history are separate versioned JSON documents. A save writes a uniquely named
temporary file in the same directory, flushes it, then replaces the destination. The temporary
file is removed if serialization or replacement fails. History is trimmed to the latest 100
results per target both when writing and when reading.

If a JSON document cannot be read, it is renamed with a UTC timestamp instead of being
overwritten. Corrupt settings cause the three defaults to be restored; corrupt history resets
only the recorded samples. The session exposes a readable warning for the dashboard.

The defaults are persisted during the first initialization. A later valid settings document,
including one with zero targets, is authoritative, so deleted defaults do not return after a
restart. MSI removal intentionally leaves this LocalAppData directory in place.
