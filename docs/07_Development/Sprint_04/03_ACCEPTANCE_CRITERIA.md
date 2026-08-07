---
title: Sprint 04 Acceptance Criteria
version: 1.0.0
status: Approved
---

# Acceptance Criteria

## AC-S04-001 — Solution builds

Given the current repository,
when Claude finishes Sprint 04,
then the complete solution builds with:

- zero errors,
- zero warnings under the repository analyzer policy.

## AC-S04-002 — Adapter discovery

Given a Windows computer with at least one network adapter,
when adapter discovery runs,
then at least the adapters visible to the chosen Windows/.NET API are returned
without requiring CLI commands.

## AC-S04-003 — Stable identity

Given an adapter is renamed in Windows,
when it is rediscovered,
then IPMan must not treat display name alone as its identity.

## AC-S04-004 — Connected state

Given an adapter's operational connection state changes,
when Windows raises the corresponding network event,
then IPMan's monitoring layer raises a change notification.

## AC-S04-005 — Adapter added

Given a USB/network adapter is added while monitoring is active,
when Windows reports network changes,
then the application can refresh discovery and observe the new adapter.

## AC-S04-006 — Adapter removed

Given an adapter is removed,
when state is refreshed,
then the removed adapter no longer appears in discovery results.

## AC-S04-007 — Safe missing values

Given an adapter has no gateway, DNS, IPv4 lease or link speed available,
when mapped,
then IPMan uses null/empty semantics defined by the domain model and does not
fabricate values.

## AC-S04-008 — No mutation

Given Sprint 04 implementation,
when source code is reviewed,
then no code path changes IP, gateway, DNS or DHCP state.

## AC-S04-009 — No aggressive polling

Given the app is idle with stable network state,
then no sub-second adapter-discovery polling loop is running.

## AC-S04-010 — Subscription cleanup

Given monitoring has been started,
when `StopMonitoring()` or disposal occurs,
then the implementation unsubscribes from Windows/.NET network change events.

## AC-S04-011 — Responsive UI

Given discovery is triggered,
then the WPF main thread is not blocked by long-running network discovery work.

## AC-S04-012 — Tests

Automated tests must cover all logic that can reasonably be isolated from the
live Windows environment.

Windows-only integration behavior that cannot be deterministic in unit tests
must be explicitly documented rather than mocked misleadingly.
