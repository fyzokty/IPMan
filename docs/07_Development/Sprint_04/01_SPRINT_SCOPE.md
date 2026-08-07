---
title: Sprint 04 Scope
version: 1.0.0
status: Approved
---

# Goal

Create a reliable, read-only adapter discovery layer.

This sprint is successful when the application can obtain trustworthy adapter
information and react to Windows network changes while remaining responsive.

# In scope

## Adapter discovery

Discover adapters visible to Windows.

For each adapter, map available information into the existing
`NetworkAdapterSnapshot` model.

Required fields where available:

- stable adapter identity,
- name,
- description,
- MAC address,
- connected/disconnected state,
- link speed,
- configuration mode if reliably determinable,
- IPv4 address,
- subnet mask,
- default gateway,
- primary DNS,
- secondary DNS.

If a value is unavailable, represent absence truthfully rather than inventing
one.

## Adapter identity

Do not use display name as the stable identifier.

The implementation must use the underlying Windows/network-interface identity
already contemplated by architecture.

## Network observation

Implement the existing `INetworkChangeMonitor` contract.

Primary trigger:
- Windows/.NET network change events.

Expected public lifecycle:
- `StartMonitoring()`
- `StopMonitoring()`

The monitor must be safe against repeated start/stop calls.

## Refresh behavior

Network event handlers must not perform expensive discovery work directly.

Events should signal/coalesce a refresh through the application/UI workflow.

Avoid duplicate refresh storms.

## UI integration boundary

Only minimal integration is required.

It is acceptable for this sprint to expose discovered adapter state to the
existing ViewModel/application shell in a temporary diagnostic/read-only form.

Do not build the final production UI.

# Out of scope

Any network mutation is prohibited in Sprint 04.
