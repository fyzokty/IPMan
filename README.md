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
canonicalized at that Domain boundary.

After that fix, the isolated `StaticToStatic` run reached production
`VerifiedSuccess`: Windows changed `10.250.0.10/24` to `10.250.0.20/24`, the WMI
IPv4 result was `0`, and gateway/DNS remained absent/automatic. The harness
failed only because rollback fidelity verification returned false. The remaining
blocker was traced to the typed rollback JSON identity contract and to comparison
against the harness's earlier recovery read instead of production Apply's exact
fresh rollback-source read. Those deterministic code paths are corrected locally;
the destructive scenario has not been rerun again and production Apply remains
closed.
