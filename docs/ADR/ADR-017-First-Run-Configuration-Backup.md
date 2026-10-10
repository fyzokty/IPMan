---
title: ADR-017 - First-Run Configuration Backup
status: Accepted
date: 2026-10-10
---

# Context

Backlog 09 requires a silent copy of the configuration present at first startup.
Pre-mutation snapshots are subject to retention and feed the latest-configuration
restore workflow (ADR-016); neither behavior is appropriate for the initial copy.
Operational data belongs under local application data (ADR-015).

# Decision

Write one document to `%LocalAppData%\IPMan\FirstRun\first-run.json`, separate
from `Backup`. Its existence alone marks completion, even if its contents become
unreadable. Never overwrite it. Create missing directories and use the existing
atomic JSON writer with a temporary file and a non-overwriting final move.

An Application service runs on a background task after the main window is shown
and adapter observation starts. It uses the same adapter reader and shared
visibility filter as the window, honoring the startup `ShowVirtualAdapters`
setting and including disconnected visible adapters. Read each adapter through
`INetworkAdapterRecoveryReader`; no configuration or route mutation is involved.

Wrap existing `RecoverySnapshot` records in a schema-versioned envelope with a
UTC capture time. Reuse `RecoverySnapshotJsonCodec` and preserve all recovery
fields and collection ordering without extending the adapter wire format.

Read failures are logged and skipped. A nonempty partial copy counts as complete.
If no adapters are visible, none can be read, or writing fails, leave no completed
file and retry at the next startup. Deleting the file also permits a new attempt;
settings resets alone do not. All failures are isolated from startup and logged
through `ICriticalLogger`, without user notifications, including log-write failures.

# Consequences

- The separate directory is exempt from ADR-016 pruning and is never enumerated
  by `JsonRecoverySnapshotRepository` or used by `RecoveryRestoreService`.
- Initial and pre-mutation backups are independent; there is no restore priority
  between them and no initial-backup UI in 1.0.
- A partial copy remains the initial copy; missing adapters are not added later.
- A crash during writing may leave a temporary file, but only the completed
  `first-run.json` prevents another attempt. Concurrent creation cannot replace
  an existing copy.
