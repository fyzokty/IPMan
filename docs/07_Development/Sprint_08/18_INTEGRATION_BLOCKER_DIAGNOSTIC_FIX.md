---
title: Sprint 08 Integration Blocker Diagnostic Fix
version: 1.0.0
status: Implemented Locally - Architect Review Pending
date: 2026-08-08
---

# Current gate

`HARNESS_READY_REAL_RUN_PENDING`

Production Apply remains closed and Sprint 08 is not integration validated.

# Real VM finding

The first explicitly opted-in isolated `StaticToStatic` attempt was
`NOT EXECUTED`. The harness stopped before rollback capture and before any
network mutation because the exact recovery state was unavailable or was not
restore-capable. The target adapter and management connectivity remained
unchanged.

# Diagnostic fix purpose

The recovery gate remains fail-closed. This change adds:

- one typed Domain evaluator for restore-capability blocking reasons;
- typed, sanitized DNS recovery-probe outcomes and numeric native result metadata;
- structured destructive-test refusal output containing only modes, counts,
  booleans, stable enums and numeric native codes;
- an explicitly opted-in, exact-GUID, read-only recovery diagnostic test.

The diagnostic path does not enumerate or select an adapter, does not invoke
the mutation service and is skipped during ordinary test execution.

# Next required review

The isolated-VM read-only diagnostic produced:

- `ManagedAdapterRead: NotFound` and managed identity `N/A`;
- WMI recovery `Success` with matching identity;
- static adapter mode and automatic DNS;
- DNS probe `Automatic` with native result `0`;
- one IPv4 address with complete subnet masks;
- zero gateways with complete gateway metrics;
- `RestoreCapability: Capable` with no blocking reasons.

Independent VM checks proved that `NetworkInterface.Id` and
`Get-NetAdapter InterfaceGuid` returned the same uppercase GUID, and an exact
.NET lookup found that adapter. The remaining blocker was application identity:
the diagnostic parser's lowercase braced GUID and Windows' uppercase GUID were
unequal in the former string-backed `NetworkAdapterId` semantics.

Parseable GUID values are now canonicalized as uppercase invariant braced GUIDs
at the `NetworkAdapterId` construction boundary. Non-GUID fake/test identities
retain their former case-sensitive string semantics. Recovery, DNS and mutation
safety behavior is unchanged.

Architect review is required before the corrected source is transferred to the
isolated VM. The destructive scenario has not been rerun. The gate remains
`HARNESS_READY_REAL_RUN_PENDING`; Sprint 08 is not `INTEGRATION_VALIDATED`.
