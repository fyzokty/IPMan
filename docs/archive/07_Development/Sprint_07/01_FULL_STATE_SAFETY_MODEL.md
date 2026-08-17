---
title: Full Network State Safety Model
version: 1.0.0
status: Approved
---

# Problem

The UI edits:
- one primary IPv4 address,
- one gateway,
- up to two IPv4 DNS servers.

Windows may contain:
- multiple IPv4 addresses,
- multiple IPv4 default gateways,
- more than two IPv4 DNS servers.

A mutation implementation must never erase state that the UI did not represent
without an explicit approved rule.

# Required read-model extension

Before static mutation is enabled, discovery must preserve:

- every IPv4 address + mask,
- every IPv4 default gateway in reported order,
- every IPv4 DNS server in reported order.

Existing convenience properties may remain:

- primary IPv4,
- Gateway,
- PrimaryDns,
- SecondaryDns.

But the complete ordered collections must also be available to safety logic.

Keep the Domain representation Windows-API independent.

# Mutation safety gates

Static mutation is BLOCKED when:

1. the adapter has more than one IPv4 address,
2. the adapter has more than one IPv4 default gateway,
3. the adapter has more than two IPv4 DNS servers.

Use explicit typed safety statuses.

Do not silently:
- remove secondary IPv4 addresses,
- collapse multiple gateways,
- drop third/fourth DNS servers.

# Why block instead of preserve automatically

The release-1.0 editor cannot express those full states.

Blindly preserving some of them while replacing others can produce an ambiguous
or unreachable configuration.

The safe v1 behavior is:
- show/read them,
- refuse destructive mutation,
- surface a clear future UI safety message.

# Existing adapter with zero values

Zero gateway is supported.

Zero DNS servers is supported.

Static mutation may proceed when the topology otherwise passes safety gates.
