---
title: Technical Risks
version: 1.0.0
status: Active
---

# R-001 — WMI behavior differences

`Win32_NetworkAdapterConfiguration` is supported Windows management surface but
individual drivers/adapters can behave differently.

Mitigation:
- typed return-code mapping,
- post-write verification,
- Windows integration test matrix.

# R-002 — DHCP gateway cleanup

WMI documentation notes that enabling DHCP does not necessarily clear all static
default gateways.

Mitigation:
- verify actual routes/state,
- use narrowly-scoped IP Helper/NetIO route cleanup if required,
- never delete unrelated routes.

# R-003 — Multiple IPv4 addresses

Simple "one textbox = entire adapter state" can accidentally overwrite unrelated
addresses.

Mitigation:
- read full state,
- identify the address managed by IPMan,
- do not implement destructive broad deletion without explicit test coverage.

# R-004 — Network events are bursty

One physical action can generate multiple events.

Mitigation:
- debounce and serialize refresh.

# R-005 — Adapter disappears mid-operation

USB adapters can be removed at any time.

Mitigation:
- stable adapter identity,
- verification,
- indeterminate failure result,
- retained rollback diagnostics.

# R-006 — Profile watcher partial writes

External editors may emit change events before the final file is stable.

Mitigation:
- debounce,
- bounded retry before marking corrupt.

# R-007 — Elevation + startup

Always-elevated applications interact differently with startup mechanisms.

Mitigation:
- Windows-startup feature implementation must be tested under UAC rather than
  assumed from a simple registry Run entry.

# R-008 — .NET 8 lifecycle

The project intentionally targets .NET 8 LTS for release 1.0. Framework upgrade
is a controlled future architecture decision, not an implicit package upgrade.
