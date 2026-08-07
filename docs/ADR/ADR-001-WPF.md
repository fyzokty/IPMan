---
title: ADR-001 - Use WPF for Desktop UI
status: Accepted
date: 2026-08-07
---

# Context

IPMan targets Windows 10 and Windows 11 only and requires mature desktop
integration, reliable MVVM support and straightforward deployment.

# Decision

Use WPF on .NET 8 for the desktop UI.

# Consequences

- Strong ecosystem and tooling.
- Mature MVVM patterns.
- Windows-only by design.
- Fluent-like styling must be provided through application resources/components
  rather than assuming WinUI controls.
