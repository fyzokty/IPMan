# State

Network engine: working. Static IPv4, gateway, DNS (manual/auto).
Snapshot capture works and writes to disk atomically.
**Restore is not implemented** — snapshot is a record, not a recovery path.

UI: read-only. No Apply button. `IStaticIpv4ApplyService` is registered in DI
but no ViewModel consumes it.

Profiles / settings persistence: not started.
CI: GitHub Actions, `windows-latest`, Release build + full test suite on push
to `main` and on PRs. Destructive tests stay skipped — no `IPMAN_*` in CI.
Elevation: settled — whole app runs `requireAdministrator`, helper split rejected
(ADR-013). Apply refuses to mutate when the process is not elevated.
Single instance: `Local\` mutex + admin-only named pipe activation. Mutation is
serialized machine-wide by a `Global\` mutex (ADR-011, ADR-014).
i18n: `Strings.resx` exists, 43 entries, single language.

# Next
1. Profile / settings persistence
2. Apply UI
3. Tray + window state persistence (PR-019, PR-020, AC-018) — activation
   currently restores from minimized only

# Constraints
- Destructive network tests are opt-in and must run only on an isolated VM adapter
- Never run network-mutating commands on the development machine
