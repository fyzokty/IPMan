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
was `NOT EXECUTED` before rollback capture or mutation. The exact-GUID read-only
diagnostic then reported WMI recovery success, automatic DNS with native result
`0`, and a restore-capable snapshot, while the managed adapter read returned
`NotFound`. The VM proved that `NetworkInterface.Id` and
`Get-NetAdapter InterfaceGuid` reported the same uppercase GUID. The blocker was
application identity semantics: equivalent uppercase/lowercase GUID text
produced unequal `NetworkAdapterId` values. Parseable GUID identities are now
canonicalized at that Domain boundary. The destructive scenario has not been
rerun and production Apply remains closed.
