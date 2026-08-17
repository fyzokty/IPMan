---
title: Codex Task - Sprint 08 Isolated Windows Mutation Validation
version: 1.0.0
status: Approved
date: 2026-08-08
---

# Role

You are the implementation engineer.

The architect owns:
- safety policy,
- release gates,
- test scenarios,
- product decisions.

Do not loosen safety requirements for convenience.

# Read before work

1. `/AGENTS.md`
2. `.claude/CLAUDE.md`
3. `.claude/ARCHITECTURE_RULES.md`
4. `docs/02_Architecture/04_Configuration_Apply_Workflow.md`
5. `docs/02_Architecture/09_Threading_and_Async.md`
6. Sprint 07 architecture/review docs
7. current Sprint 07 implementation
8. every file in `docs/07_Development/Sprint_08/`

# Objective

Create a safe, explicitly opt-in integration-validation mechanism for the real
Windows static IPv4 mutation path implemented in Sprint 07.

The harness must be able to validate a designated isolated adapter without ever
selecting an adapter heuristically.

# Important

Do not run destructive integration validation automatically.

Do not infer a target from:
- adapter name,
- "Ethernet 2",
- "vEthernet",
- Hyper-V wording,
- VMware wording,
- VirtualBox wording,
- disconnected state,
- link-local state.

The user/operator must explicitly provide the exact adapter identity.

# Sprint finish states

Sprint 08 may finish in either state:

## A. Harness implemented, real isolated run pending

Acceptable if no disposable VM/isolated adapter has been explicitly designated.

This does NOT clear the production Apply UI gate.

## B. Harness implemented and isolated real run passed

Only this state can clear the integration-validation gate for later production
Apply wiring.

Do not falsify state B from unit tests or mocks.
