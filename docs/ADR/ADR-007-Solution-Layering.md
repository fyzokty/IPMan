---
title: ADR-007 - Four Project Layered Solution
status: Accepted
date: 2026-08-07
---

# Decision

Use:
- IPMan.Domain
- IPMan.Application
- IPMan.Infrastructure
- IPMan.App

This keeps Windows/WPF dependencies at the outer layers while preserving a
small, understandable solution.
