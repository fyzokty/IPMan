---
title: Sprint 04 Architect Review
version: 1.0.0
status: Changes Requested Before Final Approval
date: 2026-08-07
---

# Sprint 04 Architect Review

## Review result

Implementation report is provisionally accepted.

Do not begin Sprint 05 yet.

Before final Sprint 04 approval, complete the targeted corrections below and
provide a code-level diff/repository for architect review.

## Accepted as-is

The following decisions are approved:

1. Use `NetworkInterface` for read-only adapter discovery.
2. Use Windows/.NET network change events as the primary refresh mechanism.
3. Keep discovery work off the UI thread.
4. Keep infrastructure Windows-specific and preserve dependency direction.
5. Keep network mutation unimplemented in Sprint 04.
6. IPv6 DNS does not need to be surfaced in release 1.0.
   IPMan release 1.0 is an IPv4 configuration product.
7. The scoped CA1707 relaxation in the test project is acceptable because the
   approved test naming convention intentionally uses underscores.
8. `NullLogger` is acceptable until the logging sprint.
9. The temporary diagnostic UI is acceptable and is not the production UI.

## Required correction 1 — Preserve all IPv4 addresses

Current report states that only one IPv4 address is represented in
`NetworkAdapterSnapshot`.

This is not sufficient for future safe mutation.

IPMan must be able to distinguish:
- the IPv4 address shown/edited as the primary release-1.0 address,
- any additional IPv4 addresses already configured on the adapter.

Requirement:

- Extend the domain/read model so discovery does not lose additional IPv4
  addresses.
- The UI may still expose only one primary IPv4 configuration in release 1.0.
- Additional addresses must remain observable by the application so later
  mutation logic cannot accidentally delete or overwrite them.
- Do not implement mutation in this correction.

Claude may choose the exact immutable domain representation, but it must remain
Windows-API independent and testable.

## Required correction 2 — Adapter visibility policy

Release 1.0 product intent is to manage all practical Windows network adapters,
including:
- physical Ethernet,
- Wi-Fi,
- USB Ethernet,
- virtual adapters,
- VPN adapters.

Therefore:

- Loopback may remain hidden.
- Tunnel adapters must NOT be globally hidden solely because their
  `NetworkInterfaceType` is `Tunnel`.
- Virtual/VPN adapters must remain discoverable by default when Windows exposes
  them.
- Do not use display-name keyword heuristics to hide adapters.

If an adapter is not configurable later, the UI should communicate that
capability explicitly rather than pretending the adapter does not exist.

Update tests accordingly.

## Required correction 3 — Low-frequency reconciliation fallback

Event-driven refresh remains the primary mechanism.

Add the already-approved lightweight reconciliation fallback:

- target interval: approximately 15 seconds,
- no sub-second polling,
- must not overlap with an active discovery pass,
- must use the existing refresh coordinator/coalescing path,
- must be cancellable/disposable,
- must not cause UI blocking.

Reason:

Network-change events are primary but the product should eventually self-heal if
an event is missed by Windows, a driver, or the application.

Add deterministic tests through an existing/appropriate time abstraction rather
than sleeping for 15 real seconds in tests.

## Required correction 4 — Repository documentation accuracy

Update root documentation that still says the repository contains no production
C#/XAML code.

At minimum update:
- `README.md`
- `CHANGELOG.md`

They must reflect that Sprint 04 now contains production read-only adapter
discovery code and tests.

## No change requested

Do not add:
- WMI mutation,
- static IP writing,
- DHCP writing,
- profile implementation,
- logging provider,
- final UI,
- tray behavior,
- notifications.

Those remain future-sprint work.

# Validation required

After corrections:

1. `dotnet build IPMan.sln`
2. `dotnet build IPMan.sln -c Release`
3. `dotnet test IPMan.sln`
4. zero production warnings/errors
5. all tests pass

# Architect review input required

After completing corrections, provide:

1. updated Sprint 04 completion report,
2. `git diff --stat`,
3. `git diff` for Sprint 04 changes, or a ZIP of the current repository if the
   diff is too large.

Do not start Sprint 05 until architect approval.
