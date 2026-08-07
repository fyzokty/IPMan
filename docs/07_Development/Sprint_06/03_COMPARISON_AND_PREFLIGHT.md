---
title: Configuration Comparison and Preflight Specification
version: 1.0.0
status: Approved
---

# Desired configuration

Use or evolve the existing `StaticIpv4Configuration` domain model without
introducing WPF dependencies.

Validation should produce a normalized configuration rather than requiring every
consumer to normalize independently.

# Comparison

Compare normalized desired configuration with current selected-adapter state.

Comparison dimensions:

- primary IPv4,
- subnet mask,
- gateway,
- primary DNS,
- secondary DNS,
- DHCP/static mode.

The result should indicate which fields differ.

The same comparison result will later support visual highlighting.

# No-change

If current actual Windows state is already equivalent to desired configuration,
preflight reports `NoChange`.

No mutation should be needed later.

# Multiple IPv4 safety

If an adapter contains additional IPv4 assignments:

- do not hide them,
- expose a typed preflight safety condition,
- do not guess how later mutation should treat them.

The current sprint remains read-only.

The architect will use the Sprint 06 implementation/report to finalize mutation
semantics for Sprint 07.

# Adapter disappearance

If the adapter identity cannot be found at preflight time, return a typed
`AdapterUnavailable` result.

Do not fall back to another adapter with a similar name.

# Current snapshot freshness

Preflight should operate from a fresh adapter read when invoked through the
application service, rather than trusting an arbitrarily old ViewModel copy.

Do not read Windows APIs from the ViewModel.

# Result model

Prefer an explicit typed result/status rather than booleans such as:

`bool IsOkay`

The result must distinguish at least:

- Ready
- NoChange
- ValidationFailed
- AdapterUnavailable
- MultipleIpv4RequiresSafetyDecision
- PotentialAddressConflict
- ProbeUnavailable/Indeterminate where appropriate

Exact naming may vary, but semantics must remain explicit.
