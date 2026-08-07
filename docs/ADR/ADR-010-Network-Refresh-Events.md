---
title: ADR-010 - Event-Driven Network Refresh
status: Accepted
date: 2026-08-07
---

# Decision

Use .NET `NetworkChange` events as the primary network refresh trigger.

Coalesce bursts with a short debounce.

Allow a low-frequency reconciliation timer only as a fallback.

Do not implement 500 ms or 1 second full-adapter polling.
