---
title: Isolated Real-Run Runbook
version: 1.0.0
status: Approved
---

# Prerequisites

Use only:
- disposable Windows 10/11 VM, or
- dedicated isolated Windows test adapter.

Recommended:
- VM checkpoint/snapshot,
- console access,
- no dependency on the tested adapter for remote access,
- known-good starting network configuration.

# Before running

1. Build Debug/Release.
2. Run normal tests.
3. Confirm integration harness is skipped by default.
4. Record the target adapter GUID manually.
5. Confirm the adapter is isolated/disposable.
6. Capture a VM snapshot/checkpoint if available.
7. Record known-good configuration.
8. Confirm `%LocalAppData%\IPMan\Backup` is writable.
9. Set explicit destructive opt-in values.
10. Run one scenario at a time.

The exact required variables, accepted scenario names, filtered command, and
evidence handling rules are documented in
`tests/IPMan.IntegrationTests/README.md`. The filter acknowledgement must match
the documented value; it does not replace using the category filter in the
command.

# During each scenario

- verify displayed target GUID,
- capture before evidence,
- run exactly one mutation,
- wait for bounded verification,
- inspect result,
- capture after evidence,
- inspect rollback snapshot.

Do not batch all destructive scenarios blindly.

# If connectivity is lost

Do not assume failure or success from connectivity alone.

Use VM console/local console.

Inspect:
- actual adapter state,
- WMI result,
- rollback/evidence data.

# After each scenario

Restore known-good configuration manually or revert VM checkpoint before the next
scenario unless the next scenario explicitly uses the prior state.

# Stop immediately if

- wrong adapter appears,
- unexpected additional IPv4/gateway/DNS appears,
- IPv6 configuration is unexpectedly altered,
- DNS/DoH properties are silently lost,
- rollback snapshot is missing/incomplete,
- mutation result says success but actual state differs.

Report evidence to architect before continuing.
