---
title: Application Lifecycle Architecture
version: 1.0.0
status: Approved
---

# Startup order

1. Windows launches elevated process through manifest/UAC.
2. Single-instance guard is acquired.
3. Critical crash marker/session state is initialized.
4. Dependency injection container is built.
5. Main window is shown promptly.
6. Settings load asynchronously where practical.
7. Profile store initializes.
8. Adapter discovery starts.
9. Network/profile watchers start.
10. UI moves from loading placeholders to actual state.

No profile is auto-applied at startup.

# Single-instance design

Use:
- named mutex for ownership,
- named pipe or equivalent local IPC signal for "activate existing instance".

Second process:
1. detects ownership failure,
2. signals first process,
3. exits.

First process:
1. receives signal,
2. restores from tray/minimized state,
3. brings main window forward.

# Shutdown

If no network mutation is running:
- save final lightweight UI state if needed,
- stop watchers,
- dispose services,
- clear clean-session/crash marker,
- exit.

If mutation is running:
- do not abandon state mid-operation.
- UI/lifecycle coordinator must either complete the critical operation or perform
  a safe cancellation path defined by the service.

# Tray

Close/minimize behavior is driven by persisted settings.

Hiding to tray must not dispose the DI container or stop adapter watchers.
