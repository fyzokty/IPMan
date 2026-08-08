# IPMan Sprint 08 — Isolated Windows Mutation Validation

This package contains Codex implementation and validation guidance.

Sprint 08 is an integration-safety sprint.

It does not wire the production Apply button.

Primary goals:

- create a deliberately opt-in destructive Windows integration harness,
- validate Sprint 07 WMI behavior on a disposable VM or isolated adapter,
- capture before/after evidence,
- verify rollback snapshot fidelity,
- prove IPv6 settings are not unintentionally damaged,
- inspect DNS mode / DNS-over-HTTPS related state before and after mutation,
- define the release gate before production UI mutation is enabled.

No normal `dotnet test` execution may mutate a real network adapter.

Current gate: `HARNESS_READY_REAL_RUN_PENDING`. The first isolated real attempt
was `NOT EXECUTED` before rollback capture or mutation because recovery
capability could not be proven. Sprint 08 now includes an explicitly opted-in,
exact-GUID, read-only diagnostic path; production Apply remains closed.
