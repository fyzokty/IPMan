---
title: Claude Code Task - Sprint 06 Network Configuration Preflight
version: 1.0.0
status: Approved
---

# Role

You are the implementation engineer.

The architect owns product and architecture decisions. Do not redesign the
application.

Before coding, read:

1. `.claude/CLAUDE.md`
2. `.claude/ARCHITECTURE_RULES.md`
3. `docs/02_Architecture/01_Technical_Architecture.md`
4. `docs/02_Architecture/04_Configuration_Apply_Workflow.md`
5. relevant networking ADRs
6. Sprint 05 final completion report
7. every file in `docs/07_Development/Sprint_06/`

Read additional requirements only when needed to resolve a concrete question.
Do not bulk-read unrelated documentation.

# Objective

Implement the safe preflight layer that will be used by the later Windows
network-mutation workflow.

At the end of Sprint 06 IPMan shall be able to determine, without changing
Windows network configuration:

- whether a requested static IPv4 configuration is valid,
- its normalized/canonical representation,
- whether it is already equivalent to current state,
- whether an adapter state requires special safety handling,
- whether the requested IPv4 address appears potentially occupied,
- why preflight cannot proceed.

# Explicitly out of scope

Do NOT implement or wire:

- `EnableStatic`,
- `EnableDHCP`,
- `SetGateways`,
- `SetDNSServerSearchOrder`,
- any WMI mutation,
- rollback write/restore,
- Apply button,
- DHCP button,
- profile persistence,
- final notifications.

Sprint 06 is read-only.

# Finish

Build Debug and Release, run all tests, complete the sprint report and stop.

Do not begin Sprint 07.
