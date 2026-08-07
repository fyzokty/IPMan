---
title: ADR-006 - Abstract Windows Network Configuration APIs
status: Accepted
date: 2026-08-07
---

# Decision

Windows networking discovery and configuration shall be isolated behind service
interfaces.

The default implementation must prefer supported Windows management APIs over
shelling out to `netsh`, PowerShell or CMD.

# Rationale

This improves testability, error handling, maintainability and avoids parsing
localized command output.

# Constraint

If a future Windows capability requires a command-line fallback, introduce it
behind the same abstraction and document the reason in a new ADR before use.
