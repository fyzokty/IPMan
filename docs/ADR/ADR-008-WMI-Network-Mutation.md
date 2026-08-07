---
title: ADR-008 - WMI Baseline for Persistent IPv4 Mutation
status: Accepted
date: 2026-08-07
---

# Context

IPMan must persistently configure static IPv4/DHCP/DNS without depending on
shell command parsing.

Microsoft documents `Win32_NetworkAdapterConfiguration` methods for these tasks.

# Decision

Use `System.Management` / WMI as the release-1.0 baseline for persistent IPv4
configuration.

Expected methods:
- EnableStatic
- SetGateways
- SetDNSServerSearchOrder
- EnableDHCP

# Consequences

WMI calls are synchronous and must be kept off UI thread.

Return codes must be translated into internal results.

Every change requires post-operation verification.

Native Windows APIs may supplement specific route behavior when WMI semantics are
insufficient.
