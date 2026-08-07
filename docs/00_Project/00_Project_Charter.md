---
title: IPMan Project Charter
version: 1.0.0
status: Approved
created: 2026-08-07
---

# IPMan Project Charter

## Purpose

IPMan exists to make Windows IPv4 network-adapter configuration fast, safe
and understandable without requiring users to work with CLI tools.

## Target users

The product is intended for:
- general Windows users,
- IT personnel,
- network specialists,
- CCTV installers,
- PLC/automation technicians,
- field service teams.

The UI must remain understandable even when the user is not a network expert.

## Supported environment

- Windows 10 x64
- Windows 11 x64
- Offline operation
- Administrator execution
- Portable and installer-based distribution

## Core principles

### Simplicity
Common tasks should require minimal interaction.

### Safety
Configuration changes must be validated, backed up and verified.

### Truthful UI
The application displays the state read from Windows, not merely the state it
attempted to apply.

### Responsiveness
Network and file operations must not block the UI.

### Maintainability
The product uses a modular MVVM architecture and explicit service boundaries.

### Documentation-first development
Approved documentation and ADRs define expected behavior.

## Initial release boundaries

Initial release focuses on IPv4 adapter management and local profile handling.

Not in initial scope:
- IPv6 configuration,
- cloud sync,
- remote computer management,
- automatic Internet-based updates,
- user accounts,
- central database.
