---
title: Claude Code Task - Sprint 05 Production Main Window UI Foundation
version: 1.0.0
status: Approved
---

# Role

You are the implementation engineer for IPMan.

The architect/product decisions already exist. Do not redesign the product.

Before changing code, read:

1. `.claude/CLAUDE.md`
2. `.claude/ARCHITECTURE_RULES.md`
3. `docs/00_Project/*`
4. `docs/01_Requirements/*`
5. `docs/02_Architecture/*`
6. relevant ADRs
7. Sprint 04 completion report
8. every file in `docs/07_Development/Sprint_05/`

# Sprint objective

Replace the temporary read-only diagnostic main window with the first
production-quality IPMan main-window foundation.

The UI shall use the already-implemented read-only adapter discovery and refresh
pipeline.

This sprint must implement:

- production adapter tab presentation,
- selected-adapter current information panel,
- selected-adapter edit-draft panel,
- copy actions for individual editable fields,
- `Mevcut Değeri Getir` behavior,
- status bar,
- loading/empty/error presentation states,
- correct selected-adapter preservation across refreshes,
- localization of all user-facing text.

# Critical restriction

Sprint 05 remains read-only with respect to Windows network configuration.

Do NOT implement:
- Apply/static IP mutation,
- DHCP mutation,
- WMI write operations,
- rollback mutation,
- profiles,
- settings persistence,
- system tray,
- Windows toast notifications,
- ping/flush/renew actions.

It is acceptable to render future action locations only when they cannot be
mistaken for working functionality. Prefer not to expose nonfunctional actions.

# Completion

Build Debug and Release, run all tests, update/add UI/ViewModel tests where
appropriate, complete the sprint completion report and stop.

Do not begin Sprint 06.
