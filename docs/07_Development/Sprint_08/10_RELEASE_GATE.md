---
title: Production Apply Release Gate
version: 1.0.0
status: Approved
---

# Production Apply remains closed

Sprint 08 implementation alone does NOT authorize wiring `Uygula`.

Current status after the first isolated VM attempt remains:

`HARNESS_READY_REAL_RUN_PENDING`

That attempt was `NOT EXECUTED` before rollback capture and mutation. Typed,
read-only recovery diagnostics must identify the blocker and receive architect
review before another destructive attempt is authorized.

The subsequent read-only diagnostic reported:

- `ManagedAdapterRead: NotFound`;
- WMI recovery `Success` with matching identity;
- `DnsMode: Automatic`, `DnsProbeStatus: Automatic`, `DnsNativeResult: 0`;
- `RestoreCapability: Capable`.

On the same VM, `NetworkInterface.Id` and `Get-NetAdapter InterfaceGuid` returned
the same uppercase GUID. The managed-reader blocker was traced to textual GUID
case/format equality in `NetworkAdapterId`, not to Windows identity, WMI recovery
or DNS recovery. GUID-shaped application identities are now canonicalized at the
Domain value-object boundary.

After identity canonicalization, one isolated `StaticToStatic` run executed.
Production returned `VerifiedSuccess`, the IPv4 WMI/native result was `0`, and
Windows changed `10.250.0.10/24` to `10.250.0.20/24`. Gateway remained absent,
IPv4 DNS remained automatic/empty, rollback JSON was created, and IPv6, route and
richer DNS observation ran. The harness failed only because
`rollbackMatchesBeforeState` was false even though manual inspection showed the
raw rollback fields matched the evidence.

Source review found two deterministic verification defects:

- default typed deserialization did not reconstruct `NetworkAdapterId` through
  its canonicalizing constructor;
- the harness compared rollback with its earlier precondition recovery read,
  not production Apply's exact second fresh rollback-source read.

The rollback v2 wire format is now read and written through one authoritative
Infrastructure codec, and a harness-only recorder captures the exact Apply source
without changing production mutation semantics. The destructive scenario has not
been rerun after these fixes, so the gate remains
`HARNESS_READY_REAL_RUN_PENDING`, not `INTEGRATION_VALIDATED`.

The architect will only open the production Apply gate after reviewing real
isolated-run evidence.

# Gate states

## NOT_READY

Any of:
- harness not implemented,
- default tests can mutate adapters,
- wrong-adapter risk exists,
- build/tests failing.

## HARNESS_READY_REAL_RUN_PENDING

Harness is safe and tested, but no approved isolated real run has completed.

This is an acceptable Sprint 08 development result.

Production Apply remains disabled/unwired.

## INTEGRATION_VALIDATED

Required real scenarios have passed with evidence and no architecture blocker
remains.

Only after architect approval may a later sprint wire production Apply.

# Blocker examples

Keep gate closed if:

- WMI provider behavior differs from documented assumptions,
- gateway clear is unreliable,
- automatic DNS restore semantics are ambiguous,
- DNS/DoH metadata is unintentionally destroyed,
- IPv6 state is changed,
- rollback snapshot does not match immediate pre-state,
- product reports success without actual equivalence.
