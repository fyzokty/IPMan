---
title: ADR-004 - Prefer Event-Driven Adapter Observation
status: Accepted
date: 2026-08-07
---

# Decision

Prefer Windows/network change events for adapter and configuration changes.

Do not use 500 ms or 1 second polling as the primary observation mechanism.

A lightweight periodic reconciliation may exist only as a reliability fallback.

# Consequences

Lower idle CPU usage and faster event response without constant polling.
