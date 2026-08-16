# Evidence Checklist

Keep private assessment evidence in `docs/evidence/local/`, which is ignored by Git. Copy only sanitised product screenshots into the public screenshots directory.

## Environment and sample installer

- Visual Studio version, desktop workload, WiX/HeatWave version
- Sample solution in Release configuration
- Sample build output, MSI, installed directory, running app, and uninstall result

## NetPulse implementation

- Solution structure and core tests
- Live dashboard with all three defaults
- Custom HTTP and DNS target workflows
- Readable degraded, offline, and error states
- Release build and dependency inventory
- WiX source, MSI output, installed DLL directory, and running installed app
- Restarted persistence and clean binary removal

## Distribution

- Passing Windows workflow
- Public release page, MSI, checksum, and notes
- WinGet manifest output, sandbox result, and pull-request link
