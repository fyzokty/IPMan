# IPMan

Windows IPv4 adapter configuration tool for Windows 10/11 x64, built on .NET 8
and WPF.

## Current state

Sprint 04 is implemented: the repository contains **production read-only adapter
discovery code and automated tests**.

Implemented today:

- adapter discovery through `System.Net.NetworkInformation.NetworkInterface`,
- stable Windows adapter identity (never the display name),
- IPv4 address, subnet mask, gateway and DNS reading, with all IPv4 addresses on
  an adapter preserved,
- event-driven network observation via `NetworkChange`, with burst coalescing and
  a low-frequency reconciliation fallback,
- a temporary read-only diagnostic window,
- unit tests for every part that can be isolated from the live Windows network.

**Not implemented yet** (later sprints): static IPv4 / DHCP mutation, profiles,
settings persistence, rollback, tray, notifications, logging provider and the
production UI. No code path in the repository changes network configuration.

## Solution layout

| Project | Responsibility |
|---|---|
| `src/IPMan.Domain` | Pure models: adapter identity, snapshots, IPv4 addresses, results. No Windows or WPF dependency. |
| `src/IPMan.Application` | Contracts and orchestration: reader/configurator/monitor interfaces, refresh coordination. References Domain only. |
| `src/IPMan.Infrastructure` | Windows integration: `NetworkInterface` reading, `NetworkChange` observation, OS mapping. |
| `src/IPMan.App` | WPF executable and composition root: views, ViewModels, DI, localization resources. |
| `tests/IPMan.Tests` | xUnit tests. Read-only: they never modify the machine's network configuration. |

Dependencies point inward: `App -> Infrastructure -> Application -> Domain`.

## Build and test

```bash
dotnet build IPMan.sln
```

```bash
dotnet test IPMan.sln
```

The build treats warnings as errors and runs the repository analyzer policy.
The application requires administrator rights at runtime (see
`src/IPMan.App/app.manifest`); building and testing do not.

## Documentation

- `docs/00_Project` — charter, vision, principles, roadmap.
- `docs/01_Requirements` — approved product requirements baseline.
- `docs/02_Architecture` — technical architecture, layering, threading, testability.
- `docs/ADR` — accepted architecture decisions.
- `docs/07_Development` — sprint scope, acceptance criteria and completion reports.

`.claude/CLAUDE.md` and `.claude/ARCHITECTURE_RULES.md` are binding for automated
contributions. Do not implement product behavior that the approved documentation
does not define.
