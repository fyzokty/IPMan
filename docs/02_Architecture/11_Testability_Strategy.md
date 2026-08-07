---
title: Architecture Testability Strategy
version: 1.0.0
status: Approved
---

# Unit-testable areas

Without Windows mutation:
- IPv4 validation,
- subnet-mask validation,
- normalization,
- desired/current comparison,
- duplicate profile naming,
- profile search/sorting,
- JSON schema/version handling,
- settings recovery,
- rollback orchestration,
- refresh coalescing logic.

# Interface fakes

Tests use fakes for:
- `INetworkAdapterReader`,
- `INetworkAdapterConfigurator`,
- `INetworkChangeMonitor`,
- future profile/settings/rollback repositories,
- clock,
- dispatcher abstraction.

# Windows integration tests

A separate Windows integration test project will be introduced later.

Destructive adapter configuration tests must not run automatically on a
developer's primary active adapter.

They require an explicitly designated test adapter/environment.

# UI tests

Core behavior belongs in ViewModels/application services so most tests do not
need to launch WPF.

UI automation is reserved for high-value end-to-end flows after the UI is stable.
