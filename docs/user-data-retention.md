# User Data Retention

NetPulse stores local settings, rolling history, corruption backups, and diagnostic logs under `%LocalAppData%\NetPulse` for the current Windows user.

The MSI owns only files in `%ProgramFiles%\NetPulse` and the Start menu shortcut. An upgrade replaces installed application files while retaining local state. Uninstallation removes the owned binaries and shortcut but intentionally leaves local state in place, allowing a later reinstall to restore the dashboard.

To erase the retained data, first uninstall NetPulse, then manually delete `%LocalAppData%\NetPulse`. This action permanently removes target definitions, history, and logs and is therefore not performed by the installer or uninstaller script.
