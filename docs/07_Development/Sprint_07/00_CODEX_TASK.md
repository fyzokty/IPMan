---
title: Codex Task - Sprint 07 Static IPv4 Mutation and Rollback Capture
version: 1.0.0
status: Approved
---

# Role

You are the implementation engineer for IPMan.

Do not redesign product behavior or architecture.

Read:

1. `/AGENTS.md`
2. `.claude/CLAUDE.md`
3. `.claude/ARCHITECTURE_RULES.md`
4. `docs/02_Architecture/03_Network_Adapter_Architecture.md`
5. `docs/02_Architecture/04_Configuration_Apply_Workflow.md`
6. `docs/02_Architecture/06_Persistence_Architecture.md`
7. `docs/02_Architecture/09_Threading_and_Async.md`
8. ADRs relevant to WMI/network mutation/local storage
9. Sprint 06 final implementation/review state
10. every file in `docs/07_Development/Sprint_07/`

# Objective

Implement the first production-capable **static IPv4 mutation engine** with:

- full pre-mutation state awareness,
- explicit safety gates,
- rollback snapshot capture before mutation,
- WMI-based static IPv4 mutation,
- fresh post-write read,
- verified result,
- partial-failure reporting,
- deterministic tests.

# Important

This sprint is allowed to contain real Windows mutation code.

However, do NOT wire it to the production UI yet.

No normal automated test may modify the developer machine's network settings.

# Out of scope

Do not implement:
- DHCP mutation,
- rollback restore execution,
- Apply button wiring,
- conflict-warning dialog,
- profiles,
- tray,
- notifications,
- theme.
