---
title: Event and Refresh Architecture
version: 1.0.0
status: Approved
---

# Purpose

Keep tabs/current state synchronized without aggressive polling.

# Primary event source

Use `System.Net.NetworkInformation.NetworkChange`:
- `NetworkAddressChanged`
- `NetworkAvailabilityChanged`

Event handlers must do minimal work and schedule a debounced refresh.

# Refresh coordinator

A single refresh coordinator should:
- coalesce bursts of Windows network events,
- avoid simultaneous full scans,
- support cancellation during shutdown,
- compare adapter identity sets,
- publish added/removed/changed results.

Recommended debounce window:
approximately 250-500 ms.

This debounce is not polling; it consolidates event bursts.

# Fallback reconciliation

A low-frequency reconciliation timer may run as a reliability fallback.

It must not use sub-second intervals.

Initial architecture target:
approximately 15 seconds while application is active.

The normal path remains immediate event-driven refresh.

# UI thread

Infrastructure/network events may originate on arbitrary threads.

ViewModels must update WPF-observed collections/properties on the WPF dispatcher
through an App-layer dispatcher abstraction or controlled UI synchronization
boundary.

Infrastructure itself must not depend on WPF Dispatcher.

# Profile directory changes

Profiles use `FileSystemWatcher`.

File events are debounced before reloading because a single write can emit
multiple filesystem events.
