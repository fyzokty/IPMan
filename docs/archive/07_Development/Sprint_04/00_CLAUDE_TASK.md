---
title: Claude Code Task - Sprint 04 Adapter Discovery
version: 1.0.0
status: Approved
---

# Role

Act as the implementation engineer for IPMan.

Do not redesign the product or architecture.

Before writing code, read:

1. `.claude/CLAUDE.md`
2. `.claude/ARCHITECTURE_RULES.md`
3. `docs/00_Project/00_Project_Charter.md`
4. `docs/01_Requirements/01_Product_Requirements.md`
5. `docs/02_Architecture/01_Technical_Architecture.md`
6. `docs/02_Architecture/03_Network_Adapter_Architecture.md`
7. `docs/02_Architecture/05_Event_Refresh_Architecture.md`
8. all ADR files relevant to networking and layering
9. every file in `docs/07_Development/Sprint_04/`

# Sprint objective

Implement the **read-only network adapter discovery and observation foundation**.

At the end of this sprint, IPMan must be able to:

- discover Windows network adapters,
- expose their current basic state through the existing application interfaces,
- detect network environment changes,
- refresh adapter state without aggressive polling,
- prove the behavior with automated tests where practical.

# Explicitly out of scope

Do NOT implement:

- static IPv4 mutation,
- DHCP mutation,
- WMI write operations,
- profile persistence,
- settings persistence,
- rollback persistence,
- system tray,
- toast notifications,
- full production UI,
- profile UI,
- ping/flush/renew quick actions.

Do not add those as “helpful extras”.

# Implementation expectations

Implement only what is necessary for Sprint 04.

Keep:
- Windows-specific reading in `IPMan.Infrastructure`,
- contracts in `IPMan.Application`,
- pure data/models in `IPMan.Domain`,
- presentation wiring in `IPMan.App`.

Use the existing interfaces when possible.

Do not replace the approved architecture.

# Completion report

When finished, report:

1. files added/changed,
2. architecture decisions followed,
3. tests added,
4. build result,
5. test result,
6. known limitations,
7. anything that needs architecture/product review.

Do not continue into Sprint 05 without approval.
