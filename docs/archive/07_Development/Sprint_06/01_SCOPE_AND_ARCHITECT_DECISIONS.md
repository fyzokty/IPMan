---
title: Sprint 06 Scope and Architect Decisions
version: 1.0.0
status: Approved
---

# In scope

## Static configuration validation

Validate:

- IPv4 address,
- subnet mask,
- optional default gateway,
- optional primary DNS,
- optional secondary DNS.

## Normalization

Produce a canonical configuration form so equivalent textual input compares
reliably.

Examples:
- trim surrounding whitespace,
- canonical IPv4 textual representation,
- normalize absent optional values consistently.

## Current vs desired comparison

Compare a desired static configuration against the selected adapter's current
snapshot.

Provide a typed result suitable for:
- no-change detection,
- later field-difference highlighting,
- later apply orchestration.

## Preflight

Preflight must evaluate the selected adapter and desired configuration without
writing to Windows.

## Best-effort conflict probe

Probe the requested IPv4 address for signs it may already be in use.

The probe is evidence only.

A negative result MUST NOT be represented as proof that the address is free.

# Architect decisions carried from Sprint 05

## Theme

Theme implementation is intentionally scheduled after core mutation safety.

Sprint 05 centralized brushes, so adding Follow Windows / Light / Dark later
does not block the network configuration core.

Do not implement theming in Sprint 06.

## Dirty drafts on adapter removal/application exit

A draft is temporary editing state, not saved user data.

For release 1.0:
- adapter removal may discard that adapter's draft,
- application exit does not require an "unsaved network draft" warning,
- switching adapters never applies the draft.

Profiles will have their own persistence semantics later.

## Multiple IPv4 addresses

Sprint 04 preserved every IPv4 assignment.

Until mutation semantics are explicitly proven safe, preflight must detect
multiple IPv4 assignments and expose that fact as a safety condition.

Do not silently discard or overwrite additional addresses.

Sprint 06 does not decide by performing a mutation.

## IPv6

IPv6 configuration remains out of scope for release 1.0.
