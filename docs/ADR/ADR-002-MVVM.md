---
title: ADR-002 - Use MVVM
status: Accepted
date: 2026-08-07
---

# Decision

Use MVVM as the UI architectural pattern.

Views must not contain network-management business logic.

ViewModels expose observable state and commands.

Services perform Windows integration and persistence.
