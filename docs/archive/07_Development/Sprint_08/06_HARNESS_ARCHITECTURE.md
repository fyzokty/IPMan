---
title: Destructive Integration Harness Architecture
version: 1.0.0
status: Approved
---

# Preferred shape

Add a dedicated integration-test project or a clearly separated integration
test category.

Do not mix destructive tests into the normal unit-test path.

Acceptable examples:

- `tests/IPMan.IntegrationTests`
- equivalent clearly named project.

# Default behavior

A plain:

`dotnet test IPMan.sln`

must remain non-destructive.

Destructive tests must:
- be skipped by default, or
- require a filter plus explicit opt-in values.

Prefer both.

# Test runner safety

The harness must display/record the exact target adapter identity before the
first mutation.

No broad "all adapters" loop.

No test should select the first configurable adapter.

# Reuse production services

The integration harness should exercise the production:
- preflight service,
- recovery reader,
- rollback repository,
- mutation coordinator,
- WMI configurator,
- static apply orchestration.

Do not duplicate mutation logic in test code.

# Independent observation

Where possible, use a read-only observer separate from the mutation
orchestration to capture before/after evidence.

This helps detect the case where the same bug exists in mutation verification
and its input mapper.

# Recovery

The harness may expose a manual/operator recovery helper, but Sprint 08 does not
yet implement the product rollback-restore feature.

Do not create a hidden automatic rollback path that would bypass later product
architecture.

If a scenario leaves the isolated adapter unusable, use VM snapshot/out-of-band
recovery or manual known-good configuration.
