---
title: Sprint 07 Architect Code Review
version: 1.0.0
status: Changes Requested
date: 2026-08-07
---

# Review result

The Sprint 07 structure is strong, but it is not approved yet.

Four corrections are required before the static mutation foundation can be
accepted.

Do not wire Apply to the production UI and do not begin Sprint 08.

# Accepted implementation

The following are approved:

- exact adapter identity lookup through SettingID,
- no display-name fallback,
- full IPv4 address/gateway/DNS collections,
- safety blocking for multiple IPv4 addresses, multiple gateways, and >2 IPv4
  DNS servers,
- rollback persisted before the native mutation call,
- same-directory temp + final move JSON persistence,
- no shell/PowerShell/netsh fallback,
- documented SetGateways clear sentinel,
- parameterless SetDNSServerSearchOrder for automatic/DHCP DNS semantics,
- per-step mutation result model,
- partial failure is never success,
- bounded post-mutation verification,
- no production Apply-button wiring,
- non-destructive default tests.

# Required correction 1 — EnableStatic return code 81

## Problem

The current generic WMI result mapper treats every code other than 0 and 1 as
failure.

Microsoft documents a special EnableStatic behavior:

When the adapter is already configured with a static address, EnableStatic can
return:

`81 - Unable to configure DHCP service`

while still successfully setting the new static configuration.

Therefore return-code semantics are method-specific.

## Required behavior

Do not globally make 81 successful for every WMI method.

For EnableStatic:

- when the fresh pre-mutation adapter state proves the adapter was already
  Static, code 81 must be represented as a successful/provisional successful
  EnableStatic step so gateway/DNS steps may continue and final fresh
  verification remains authoritative;
- when prior mode is DHCP or Unknown, do not blindly reinterpret 81 as success.

The low-level mutation plan/session may need enough expected pre-mutation state
to make this distinction.

Final product success still requires actual-state verification.

## Tests

Add tests proving:

1. Static -> Static + EnableStatic returns 81:
   - IPv4 step is not treated as terminal failure,
   - gateway/DNS continue,
   - technical code 81 is retained.

2. DHCP/Unknown -> EnableStatic returns 81:
   - it is not blindly accepted as normal success unless Microsoft-documented
     behavior can prove it safe.

3. 81 remains an ordinary failure for other WMI methods.

Primary reference:

Microsoft Learn:
`Win32_NetworkAdapterConfiguration.EnableStatic`

The remarks explicitly describe the already-static code-81 behavior.

# Required correction 2 — Fresh state immediately before rollback capture

## Problem

Sprint 06 preflight performs:
- a fresh adapter read,
- validation/comparison,
- potentially a bounded conflict probe.

Sprint 07 then uses that preflight snapshot for topology safety and rollback
capture.

This creates a stale-state window before the first mutation.

A DHCP lease, Windows network event, driver behavior, or another administrator
could change:
- IPv4,
- gateway,
- DNS,
- mode,
- topology

between the preflight read and rollback capture.

Rollback must describe the state immediately before IPMan changes Windows, not an
older preflight observation.

## Required workflow

After preflight/warning handling succeeds, but before rollback capture:

1. fresh-read the exact adapter identity again;
2. re-evaluate:
   - adapter availability,
   - mode,
   - current-vs-desired comparison,
   - multiple IPv4 safety,
   - multiple gateway safety,
   - DNS count safety;
3. use this second fresh snapshot as the rollback source;
4. build the mutation plan from this second fresh snapshot;
5. if this second read is now equivalent to desired state, return NoChange
   without rollback/mutation;
6. if it now violates a safety gate, stop without mutation.

Do not silently fall back to the older preflight snapshot.

The conflict probe remains best-effort; it does not need to be converted into an
authoritative ownership lock.

## Tests

Add deterministic tests where state changes between:
- preflight snapshot,
- pre-mutation refresh.

Cover:
- adapter disappears,
- adapter becomes NoChange,
- second IPv4 appears,
- multiple gateway appears,
- third DNS appears,
- DHCP/static mode changes,
- rollback content is taken from the second fresh snapshot.

# Required correction 3 — Shared mutation coordination

## Problem

`StaticIpv4ApplyService` currently owns a private SemaphoreSlim.

That serializes calls to this single service instance, but it cannot coordinate
future:
- DHCP mutation,
- rollback restore,
- other network mutation services.

The approved architecture states that those operations must not race.

## Required architecture

Move the mutation serialization responsibility into a small shared
Application-layer coordination abstraction/service.

Examples of acceptable intent:

- `INetworkMutationCoordinator`
- `INetworkMutationGate`
- equivalent architecturally clear name.

Register the production implementation as a singleton.

Static apply must acquire this shared gate.

Future DHCP and rollback services will use the same instance.

Do not use a static mutable global.

Tests must prove two overlapping static applies remain serialized. Add a focused
test for the coordinator itself if useful.

# Required correction 4 — Rollback snapshot must be restore-capable

## Problem

The current rollback document captures:
- DHCP/static mode,
- IPv4 address/mask values,
- gateway addresses,
- DNS server addresses.

This is not necessarily sufficient to reconstruct the exact semantics that
IPMan is changing.

Two important examples:

## DNS source

A DHCP-addressed adapter can use:
- DHCP/automatic DNS, or
- manually configured DNS.

A list of current DNS server addresses does not by itself prove which source was
configured.

A future rollback must not convert:
- DHCP-provided DNS into permanent manual DNS,
or
- manual DNS into automatic DNS.

## Gateway metric

The mutation implementation sets `GatewayCostMetric = 1`.

If an existing static gateway had a non-default metric, the current snapshot
does not retain it, so exact rollback cannot reconstruct it.

## Required action

Before Sprint 07 approval, make rollback capture preserve enough state to restore
every setting the Sprint 07 mutator can alter.

At minimum investigate and model:

- DNS source/configuration mode:
  - Automatic/DHCP
  - Manual
  - Unknown when it cannot be determined safely
- gateway metric corresponding to the captured gateway where available.

Do not guess these values.

Use a Windows-specific Infrastructure reader if the cross-platform
NetworkInterface abstraction cannot provide them.

Keep Domain/Application models Windows-API independent.

If Microsoft/Windows APIs cannot provide a reliable signal, report that as an
architect blocker instead of fabricating restore semantics.

Increment the rollback schema version if the persisted schema materially
changes.

# IPv6 non-interference — mandatory integration gate, not normal unit test

IPv6 configuration remains out of product scope.

Microsoft documents that Win32_NetworkAdapterConfiguration has both IPv4 and
IPv6 awareness.

Before production Apply is enabled, isolated Windows integration validation must
prove that the chosen EnableStatic / SetDNSServerSearchOrder path does not
silently destroy unrelated IPv6 configuration.

Do not run this test on the developer's normal adapter.

Use:
- disposable VM, or
- explicitly designated isolated adapter.

If IPv6 state is affected, stop and return to the architect before UI wiring.

This integration gate does not require a destructive default unit test.

# Validation after corrections

Run:

- `dotnet build IPMan.sln`
- `dotnet build IPMan.sln -c Release`
- `dotnet test IPMan.sln`

Expected:
- 0 warnings
- 0 errors
- all tests pass

Do not perform real mutation on the normal development adapter.

Leave changes uncommitted for final review.
