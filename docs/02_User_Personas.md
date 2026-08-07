---
title: IPMan User Personas
version: 1.0.0
status: Approved
---

# User Personas

## P1 — General Windows User

Needs to switch IPv4 configuration without understanding command-line syntax.

Priorities:
- clear connected/disconnected state,
- simple input,
- obvious error messages,
- safe defaults.

## P2 — IT / Network Technician

Frequently moves between subnets and devices.

Priorities:
- fast adapter switching,
- profiles,
- current/profile comparison,
- copy diagnostics/network information,
- import/export,
- rollback.

## P3 — PLC / Automation Technician

Connects directly to industrial devices using multiple static subnets.

Priorities:
- repeatable static profiles,
- fast favorite access,
- disconnected-adapter configuration,
- no Internet dependency,
- IP conflict warning.

## P4 — CCTV / Field Service Technician

Moves between customer networks and USB/Ethernet adapters.

Priorities:
- automatic adapter detection,
- clear adapter identification,
- portable app option,
- fast profile reuse,
- low-friction tray behavior.

## Design implication

The UI must serve expert speed without requiring expert knowledge.
Advanced information may be visible, but the primary workflow must remain
simple.
