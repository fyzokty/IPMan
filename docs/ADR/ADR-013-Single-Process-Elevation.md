---
title: ADR-013 - Keep Elevation in a Single Process
status: Accepted
date: 2026-08-18
---

# Decision

IPMan continues to run as one `requireAdministrator` process. Splitting the
privileged work into a separate helper process was evaluated and rejected.

This confirms and does not supersede [ADR-005](ADR-005-Administrator-Execution.md).

# Context

The privileged surface is one interface. Every Windows call that needs
administrator rights is reached through `INetworkAdapterConfigurator.ApplyStaticAsync`:
WMI `EnableStatic`, `SetGateways` and `SetDNSServerSearchOrder`,
`iphlpapi!SetInterfaceDnsSettings`, and — for gateway clearing — an `MSFT_NetRoute`
delete plus `DeleteIpForwardEntry2`.

Everything the UI renders today runs unelevated: adapter discovery, WMI property
reads, the DNS truth-source read, route table reads and the ICMP conflict probe.
The current manifest therefore buys nothing a user can reach, because no
ViewModel consumes the mutation path yet.

# Rationale

Elevation is a standing product commitment, recorded in PR-032, BR-020,
NFR-SEC-001/002, ADR-005, and the approved Security and Application Lifecycle
architecture documents. Nothing in the requirements asks for privilege
minimisation; the phrase "least privilege" does not appear, no persona is
described as a standard user, and no use case runs without administrator rights.

A Windows service helper is ruled out by distribution: PR-031 requires both a
portable and an installer build, and ADR-012 requires identical persistence
semantics between them. A service running as LocalSystem resolves
`%LocalAppData%` under `systemprofile`, which would silently write recovery
snapshots where the user cannot reach them.

An unelevated UI with an on-demand elevated child would work, but its only
benefit is deferring the UAC prompt past launch — a benefit no requirement asks
for, paid for with an IPC channel that is itself a privilege boundary. ADR-011
deliberately limits the existing local IPC to a minimal activation message; a
pipe that accepts mutation requests from a medium-integrity client is the
classic UAC bypass shape and would need its own threat model.

# Consequences

The user sees a UAC prompt at every launch, including when they only want to
read adapter state. A standard user cannot run IPMan at all. Both are accepted.

The manifest guarantees elevation only for a normal launch. The process can
still be started unelevated, so `StaticIpv4ApplyService` refuses to mutate when
the process token is not elevated, returning `SafetyBlocked` with
`StaticIpv4SafetyBlock.NotElevated` before acquiring the mutation lease or
producing any side effect.

If this decision is revisited, the correct seam is `INetworkAdapterConfigurator`
— a one-line substitution at `src/IPMan.App/App.xaml.cs:79`. It is not
`IStaticIpv4ApplyService`, which would drag the LocalAppData snapshot write, a
seven-field result graph with three custom collection types, and the caller's
user-confirmation flags across the boundary.

# Prerequisite this decision exposes

ADR-011 is accepted but unimplemented: there is no single-instance mutex, and
`NetworkMutationCoordinator` guards mutation with an in-process `SemaphoreSlim`.
Two concurrently running copies would not exclude each other. This is harmless
while no UI can trigger a mutation, and becomes a correctness defect the moment
the Apply UI ships. It must be resolved before then.
