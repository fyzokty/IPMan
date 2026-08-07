# Changelog

## Sprint 04 — Adapter discovery (2026-08-07)

First production code in the repository. Read-only only: nothing in this release
changes Windows network configuration.

### Added

- `IPMan.Domain`: `Ipv4AddressAssignment` and `Ipv4AddressCollection`;
  `NetworkAdapterSnapshot` now carries every IPv4 address configured on an
  adapter alongside the primary one.
- `IPMan.Application`: `IAdapterRefreshCoordinator` / `AdapterRefreshCoordinator`
  with event coalescing, added/removed/changed reporting and a low-frequency
  (~15 s) reconciliation fallback; `IDelayProvider` time abstraction.
- `IPMan.Infrastructure`: `NetworkAdapterReader`, `SystemNetworkInterfaceProbe`,
  `NetworkAdapterMapper`, `AdapterDiscoveryFilter`, `NetworkChangeMonitor` and
  `SystemDelayProvider`. Adapter reading uses `NetworkInterface`; observation
  uses `NetworkChange`. No `netsh`, PowerShell, CMD or WMI.
- `IPMan.App`: dependency registration for discovery/observation, a UI dispatcher
  boundary, Turkish localization resources and a temporary read-only diagnostic
  window.
- `tests/IPMan.Tests`: xUnit project covering mapping, adapter visibility,
  discovery, monitor lifecycle, refresh coalescing and reconciliation.

### Changed after architect review

- All IPv4 addresses on an adapter are preserved instead of only the primary one.
- Adapter visibility: only software loopback is hidden. Tunnel, virtual and VPN
  adapters remain discoverable, and display-name heuristics are never used.
- Added the approved low-frequency reconciliation fallback on top of the
  event-driven refresh path.
- Root documentation updated to describe the repository accurately.

## Sprint 04 guidance package 1.0.0 — 2026-08-07

Created implementation guidance for read-only adapter discovery, adapter
identity, network-change monitoring, resource cleanup, threading constraints,
tests and architecture review. That package contained no production code.
