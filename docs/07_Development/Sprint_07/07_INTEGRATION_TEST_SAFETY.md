---
title: Windows Integration Test Safety
version: 1.0.0
status: Approved
---

# Unit tests

Normal `dotnet test` must remain safe and must not alter real adapter settings.

Use fakes for:
- preflight,
- rollback repository,
- configurator,
- adapter reader,
- delay/reconciliation.

# Concrete WMI implementation tests

Do not write a normal automated test that changes the developer's active
Ethernet/Wi-Fi adapter.

If an integration harness is added, it must be:
- opt-in,
- clearly named destructive/integration,
- skipped by default,
- require an explicitly designated test adapter identity,
- refuse to run when the required safety opt-in is missing.

Do not infer "safe test adapter" from display-name keywords.

# Manual validation

Real mutation validation should be performed only on:
- a disposable VM adapter, or
- a dedicated isolated test adapter.

Before a real manual mutation:
- record current config,
- ensure alternate access/recovery exists,
- confirm rollback snapshot was written.

Do not require real mutation to make the default unit-test suite pass.
