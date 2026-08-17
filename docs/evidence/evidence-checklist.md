# Evidence Checklist

Keep private assessment evidence in `docs/evidence/local/`, which is ignored by Git.
Copy only sanitized product screenshots into `docs/evidence/task-1/screenshots`.

## Evidence naming

- Use `YYYY-MM-DD-shortdesc.md/.txt/.png` for every capture.
- Keep assignment-stage captures grouped under:
  - `docs/evidence/task-1/task-1.1`
  - `docs/evidence/task-1/task-1.2`
  - `docs/evidence/task-1/task-1.3`
  - `docs/evidence/task-1/task-1.4`
  - `docs/evidence/task-1/troubleshooting`

## Mandatory evidence by task

### Task 1.1

- Sample app build output in Release.
- Task 1.1 MSI install, launch, and uninstall.
- Success message shown to console.

### Task 1.2 / NetPulse milestone

- [x] NetPulse MSI install on Windows 11 x64.
- [x] Live dashboard with default targets and a manual check result.
- [x] Clean application shutdown, restart, and persistence check.
- [x] Scripted uninstall record with retained LocalAppData.

### Task 1.3 / Deployment checks

- [x] Passing clean Windows workflow run linked in the evidence index.
- [x] Runtime dependency inventory check for Program Files.
- [x] Automated 1.0.0 to 1.0.1 replacement test implemented.
- [x] Automated uninstall and `%LocalAppData%\NetPulse` retention checks implemented.

### Task 1.4 / Distinction route

- Release artifact checksums and runtime requirement.
- Local `winget validate` output.
- WinGet PR link and sandbox acceptance evidence.
