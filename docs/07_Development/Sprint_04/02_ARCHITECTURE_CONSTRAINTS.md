---
title: Sprint 04 Architecture Constraints
version: 1.0.0
status: Approved
---

# Mandatory constraints

## Layering

`IPMan.Domain`
- no WPF,
- no Windows management APIs,
- no filesystem,
- no DI framework dependency unless already approved.

`IPMan.Application`
- contracts and orchestration only,
- no `System.Management`,
- no WPF.

`IPMan.Infrastructure`
- Windows adapter discovery,
- Windows network-event implementation,
- OS-specific mapping.

`IPMan.App`
- dependency registration,
- ViewModel/UI presentation only.

## Adapter reading

Prefer the managed .NET network-information APIs for ordinary adapter reads.

Do not introduce WMI solely because it exists.

WMI may only supplement data that the managed API cannot provide reliably, and
that addition must remain in Infrastructure.

## Threading

Do not block the WPF UI thread.

Do not scatter `Task.Run` throughout ViewModels.

If synchronous OS APIs need background execution, isolate that decision inside
the infrastructure/application boundary.

## Events

Do not use 500 ms or 1 second polling as the primary monitoring mechanism.

Use event-driven network observation.

A future low-frequency reconciliation timer is allowed by architecture, but it
is not required unless Claude can justify it with a concrete reliability need.

## Resource lifetime

Any static/global Windows event subscriptions must be unsubscribed.

Avoid event-handler leaks.

`INetworkChangeMonitor.Dispose()` must leave no active subscriptions.

## Error behavior

A single unreadable/unavailable adapter property must not crash discovery of
other adapters.

Expected failures should be handled per-adapter where possible.

## Dependencies

Do not add new NuGet packages unless the existing .NET APIs are insufficient.

If a package is added:
- explain why,
- document it in the completion report,
- do not add a UI framework/theme package during this sprint.
