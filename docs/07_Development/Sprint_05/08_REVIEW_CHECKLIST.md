---
title: Sprint 05 Architect Review Checklist
version: 1.0.0
status: Approved
---

# Architecture

- [ ] No network mutation implementation.
- [ ] No Windows networking API calls from App ViewModels.
- [ ] Domain snapshots remain immutable facts.
- [ ] Edit drafts are separate from current snapshots.
- [ ] UI dispatcher boundary remains intact.
- [ ] No unnecessary third-party UI framework was added.

# Adapter behavior

- [ ] Tabs keyed by adapter identity.
- [ ] Selected identity preserved across refresh.
- [ ] Adapter removal handled.
- [ ] Added adapters appear without restarting.
- [ ] Additional IPv4 addresses remain available/displayed.

# Draft behavior

- [ ] Per-adapter drafts are independent.
- [ ] Dirty draft not silently overwritten by refresh.
- [ ] Get Current Values is explicit reset.
- [ ] Switching tabs does not apply anything.

# UI

- [ ] Connection status not color-only.
- [ ] Missing values are clear.
- [ ] Layout works at minimum window size.
- [ ] No Canvas/fixed-pixel layout abuse.
- [ ] Status bar implemented.
- [ ] Turkish strings are localized.
- [ ] No fake working Apply/DHCP behavior.

# Quality

- [ ] Debug build clean.
- [ ] Release build clean.
- [ ] All tests pass.
- [ ] No broad analyzer suppressions.
- [ ] `git diff` contains Sprint 05 only.
