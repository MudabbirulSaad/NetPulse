# Definition of Done

A task is complete when:

- behaviour satisfies its written acceptance criteria;
- deterministic tests cover success, failure, and cancellation paths;
- Release configuration builds with zero warnings;
- network work never blocks the WPF UI thread;
- logs and UI messages contain no secrets;
- relevant documentation and evidence prompts are updated;
- the change is committed as one coherent Conventional Commit; and
- the public branch passes Windows CI.

Installer and release tasks additionally require clean-machine installation, installed-file inspection, application launch, persistence, uninstall, and public-link checks.
