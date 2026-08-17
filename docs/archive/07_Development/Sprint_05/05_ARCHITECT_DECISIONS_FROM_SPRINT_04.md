---
title: Architect Decisions Carried from Sprint 04
version: 1.0.0
status: Approved
---

# Decision 1 — Configurability flag

Do not add a `configurable` capability flag merely for Sprint 05 UI.

Configurability depends on the mutation strategy and Windows API behavior.

It will be defined in the network-mutation architecture sprint.

Sprint 05 should display all discoverable non-loopback adapters.

# Decision 2 — Per-address origin/lifetime metadata

The current preservation of all IPv4 address + subnet-mask assignments is
sufficient for Sprint 05 presentation.

Do not extend the domain with DHCP/manual origin, lifetime or DAD metadata only
for UI purposes.

Before mutation is implemented, the architect will explicitly evaluate which
per-address metadata is required to prevent destructive changes.

# Decision 3 — IPv6 DNS

IPv6 DNS does not need to be shown in release 1.0.

IPMan release 1.0 is focused on IPv4 configuration.

# Decision 4 — Reconciliation

The ~15 second fallback reconciliation introduced in Sprint 04 remains approved.

Sprint 05 must consume its refresh results normally and must not add a second UI
polling mechanism.
