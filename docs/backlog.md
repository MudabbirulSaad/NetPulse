# Backlog

| ID | Task | Depends on | Completion signal |
|---|---|---|---|
| NP-001 | Repository and solution setup | - | Release build and tests run locally |
| NP-002 | Domain model and validation | NP-001 | Core rules are covered by tests |
| NP-003 | ICMP probe | NP-002 | Async reachability result with cancellation |
| NP-004 | HTTP probe | NP-002 | Typed status, latency, and failure results |
| NP-005 | DNS resolver probe | NP-002 | Explicit resolver query and response details |
| NP-006 | Monitoring session | NP-003-005 | Recurring checks do not overlap |
| NP-007 | Dashboard | NP-006 | Default targets display live state |
| NP-008 | Target management | NP-007 | Add/edit/delete/enable/test flows work |
| NP-009 | Health classification | NP-003-005 | Results map consistently to UI states |
| NP-010 | Rolling metrics | NP-006 | Current/min/max/average and graph data exist |
| NP-011 | JSON persistence | NP-008 | Targets and history survive restart |
| NP-012 | Logging and error handling | NP-003-011 | Expected failures do not crash the app |
| NP-013 | Test-suite expansion | NP-002-012 | Core, adapters, session, and view models covered |
| NP-014 | Network test checklist | NP-003-012 | Opt-in real-network scenarios documented |
| NP-015 | Sample application MSI | NP-001 | Build/install/run/uninstall path exists |
| NP-016 | NetPulse MSI | NP-012, NP-015 | MSI installs and launches NetPulse |
| NP-017 | Runtime dependency inventory | NP-016 | Required DLLs are documented and packaged |
| NP-018 | Clean-machine installation | NP-017 | Installed application runs without missing files |
| NP-019 | Windows CI | NP-013, NP-016 | Build/test/MSI workflow passes |
| NP-020 | User and architecture docs | NP-018, NP-019 | Repository is reproducible and understandable |
| NP-021 | Evidence inventory | all milestones | Required evidence has an indexed location |
| NP-022 | GitHub 1.0.0 release | NP-018-021 | Public MSI, checksum, and notes exist |
| NP-023 | WinGet submission | NP-022 | Public manifest pull request exists |
