---
title: Best-Effort IPv4 Conflict Probe
version: 1.0.0
status: Approved
---

# Purpose

Provide a lightweight warning signal before a future static IP apply.

# Safety statement

The probe can indicate:

`The requested IP may already be in use.`

It must never claim:

`This IP is definitely free.`

Silence/no response is inconclusive because hosts and firewalls may ignore ICMP.

# Baseline probe

A short ICMP echo probe is acceptable as the baseline implementation.

Keep it behind an Application abstraction so:
- tests do not send packets,
- future ARP/neighbour checks can supplement it without changing ViewModels.

# Result semantics

The probe should distinguish at minimum:

- response observed / potential conflict,
- no response / inconclusive,
- probe unavailable/error / indeterminate,
- cancelled.

Do not convert network exceptions into application crashes.

# Timeout

Keep the probe bounded and reasonably short.

Do not block the WPF UI thread.

Do not run repeated aggressive ping loops.

One small bounded preflight sequence is sufficient for Sprint 06.

# Special case

If the requested address is already the selected adapter's current primary IPv4,
the no-change/current comparison takes precedence; do not warn that the adapter
itself is an address conflict.
