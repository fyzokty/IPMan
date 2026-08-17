---
title: Static IPv4 Mutation Contract
version: 1.0.0
status: Approved
---

# Infrastructure baseline

Use the already-approved Windows WMI baseline:

`Win32_NetworkAdapterConfiguration`

Expected methods:

- `EnableStatic`
- `SetGateways`
- `SetDNSServerSearchOrder`

Use `System.Management`.

Do not use:
- netsh,
- PowerShell,
- CMD.

# Adapter mapping

Map `NetworkAdapterId` to the exact WMI adapter configuration using a stable
Windows identity.

Do not match only by display name or description.

If the identity cannot be matched uniquely:
- return adapter unavailable/mapping failure,
- do not mutate another adapter.

# Return codes

WMI method return codes must be translated into typed internal results.

At minimum distinguish:
- success,
- success requiring restart/reboot if reported,
- known operational failure,
- unexpected WMI/management failure.

Do not expose numeric WMI return codes directly to ViewModels.

Retain technical code for diagnostics.

# Static mutation order

The low-level configurator should apply only the requested static configuration
for an adapter that has already passed topology/preflight safety.

Expected logical order:

1. static IPv4 address + mask,
2. default gateway semantics,
3. DNS semantics.

The application orchestration layer, not the WMI class, owns rollback and final
verification.

# Partial failure

If step 1 succeeds and step 2/3 fails:
- return a typed partial failure,
- do not report success,
- retain rollback snapshot,
- final application service must fresh-read actual Windows state.

Do not attempt an undocumented automatic rollback inside the low-level WMI
adapter.

# Clearing gateway

Desired `Gateway == null` means:
- the resulting managed static configuration must have no IPv4 default gateway
  represented by IPMan.

Before implementing the concrete WMI call, verify the documented
`SetGateways` semantics from Microsoft documentation.

If the method cannot deterministically clear the gateway through the approved
API:
- do not invent a shell fallback,
- report a blocker to the architect.

# DNS semantics

For release 1.0 static configuration:

- primary+secondary supplied -> set in that order,
- only primary supplied -> set one server,
- both empty -> clear the manual IPv4 DNS server search order / return DNS to
  automatic source semantics supported by Windows.

Secondary without primary is already invalid from Sprint 06.

Before concrete implementation, verify the documented
`SetDNSServerSearchOrder` null/empty semantics.

If Windows/WMI semantics do not support the requirement safely:
- stop and report the mismatch,
- do not guess.

# Documentation verification

For WMI method signatures/return semantics, use Microsoft Learn as the primary
technical source.

Do not substitute third-party snippets for undocumented behavior.
