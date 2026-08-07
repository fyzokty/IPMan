---
title: Sprint 07 Acceptance Criteria
version: 1.0.0
status: Approved
---

# AC-S07-001

Debug and Release builds have zero warnings and zero errors.

# AC-S07-002

Discovery/domain state preserves all IPv4 gateways and all IPv4 DNS servers.

# AC-S07-003

Static mutation is blocked when the selected adapter has:
- multiple IPv4 addresses,
- multiple IPv4 default gateways,
- more than two IPv4 DNS servers.

# AC-S07-004

No low-level mutation is attempted when preflight fails or topology is blocked.

# AC-S07-005

No-change returns without rollback capture and without WMI mutation.

# AC-S07-006

Potential conflict requires explicit caller confirmation before mutation.

# AC-S07-007

Rollback snapshot is successfully persisted before the first mutation call.

# AC-S07-008

Rollback persistence failure prevents mutation.

# AC-S07-009

Concrete static mutation uses approved Windows management APIs and no shell
commands.

# AC-S07-010

A low-level partial failure cannot produce product-level success.

# AC-S07-011

After mutation, actual adapter state is re-read.

# AC-S07-012

VerifiedSuccess is returned only when actual Windows state equals normalized
desired state.

# AC-S07-013

If verification never matches within the bounded window, result is
VerificationFailed/indeterminate, not success.

# AC-S07-014

Adapter disappearance during apply cannot cause mutation of another adapter.

# AC-S07-015

Rollback snapshot remains available after verified success.

# AC-S07-016

Automated default tests never change the machine's real network configuration.

# AC-S07-017

All Sprint 04-06 tests remain passing.

# AC-S07-018

No Apply button is wired to the mutation engine in Sprint 07.
