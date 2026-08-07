---
title: IPMan Technical Architecture
version: 1.0.0
status: Approved
date: 2026-08-07
---

# 1. Architecture goals

IPMan architecture must make Windows networking operations reliable and
testable while keeping WPF presentation concerns isolated from OS integration.

The architecture must support:
- Windows 10/11 x64,
- .NET 8,
- administrator execution,
- multiple adapters,
- asynchronous reads/writes,
- event-driven refresh,
- JSON profile/settings persistence,
- rollback,
- offline operation,
- future localization.

# 2. Solution structure

The initial solution contains four production projects:

## IPMan.Domain

Pure business/domain data.

Responsibilities:
- network adapter identity,
- immutable current-state snapshots,
- static IPv4 configuration model,
- configuration mode,
- operation-result primitives.

Must not reference:
- WPF,
- System.Management,
- filesystem APIs,
- Windows-only UI APIs.

## IPMan.Application

Use-case boundaries and abstractions.

Responsibilities:
- interfaces for reading/configuring adapters,
- network-change observation contract,
- orchestration contracts introduced in later sprints,
- profile/settings/rollback contracts introduced with persistence.

References:
- Domain only.

## IPMan.Infrastructure

Windows and local-computer integrations.

Responsibilities:
- NetworkInterface-based adapter reading,
- WMI-based persistent IPv4/DHCP/DNS mutation,
- Windows native route operations if needed,
- network change event subscription,
- JSON/file persistence,
- critical technical logging,
- operating-system integration.

References:
- Application,
- Domain.

## IPMan.App

WPF executable and composition root.

Responsibilities:
- Views,
- ViewModels,
- resource dictionaries,
- localization resources,
- dialog/tray presentation,
- dependency injection composition,
- application lifetime.

References:
- Application,
- Domain,
- Infrastructure.

# 3. Dependency rule

Dependencies point inward:

`App -> Infrastructure -> Application -> Domain`

App may reference Application and Domain directly for presentation models.

Domain never references higher layers.

Application never references WPF or Infrastructure.

# 4. Read/write separation

Reading network state and mutating network state are separate contracts.

This is deliberate because:
- reading should be frequent and safe,
- mutation is privileged and failure-prone,
- verification requires a fresh read after every mutation,
- tests can fake reads and writes independently.

# 5. Windows is authoritative

The application must never assume an API call means a requested configuration
is active.

After mutation:
1. request change,
2. wait/reconcile as needed,
3. read Windows state,
4. compare requested state with actual state,
5. only then report verified success.

# 6. UI architecture

WPF follows MVVM.

Views:
- layout,
- bindings,
- visual states,
- presentation-only event wiring.

ViewModels:
- expose state,
- execute commands,
- call application services,
- never use WMI directly,
- never parse/write profile files directly.

Infrastructure:
- never directly manipulates WPF controls.

# 7. Dependency injection

`IPMan.App` is the composition root.

Use constructor injection.

Do not use service locator patterns.

Do not make ViewModels responsible for assembling concrete infrastructure.

# 8. Localization

All future user-facing text is loaded from WPF localization resources.

Brand name `IPMan` may remain literal.

Release 1.0 translation set is Turkish.

# 9. External process policy

Do not use:
- `netsh`,
- PowerShell,
- CMD

as the normal implementation path.

If an unsupported edge case eventually requires external-process fallback, it
must:
- be isolated behind an infrastructure interface,
- never expose command output parsing to ViewModels,
- be documented in an ADR before use.
