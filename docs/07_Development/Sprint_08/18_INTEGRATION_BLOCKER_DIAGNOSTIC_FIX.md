---
title: Sprint 08 Integration Blocker Diagnostic Fix
version: 1.0.0
status: Implemented Locally - Architect Review Pending
date: 2026-08-08
---

# Current gate

`HARNESS_READY_REAL_RUN_PENDING`

Production Apply remains closed and Sprint 08 is not integration validated.

# Real VM finding

The first explicitly opted-in isolated `StaticToStatic` attempt was
`NOT EXECUTED`. The harness stopped before rollback capture and before any
network mutation because the exact recovery state was unavailable or was not
restore-capable. The target adapter and management connectivity remained
unchanged.

# Diagnostic fix purpose

The recovery gate remains fail-closed. This change adds:

- one typed Domain evaluator for restore-capability blocking reasons;
- typed, sanitized DNS recovery-probe outcomes and numeric native result metadata;
- structured destructive-test refusal output containing only modes, counts,
  booleans, stable enums and numeric native codes;
- an explicitly opted-in, exact-GUID, read-only recovery diagnostic test.

The diagnostic path does not enumerate or select an adapter, does not invoke
the mutation service and is skipped during ordinary test execution.

# Next required review

Architect review is required before transferring the source to the isolated VM.
On the VM, the read-only recovery diagnostic must be run before any second
destructive attempt. A later fix may address the proven underlying reader issue;
this diagnostic task does not guess DNS semantics or weaken recovery safety.
