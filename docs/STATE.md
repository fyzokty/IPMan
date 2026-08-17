# State

Network engine: working. Static IPv4, gateway, DNS (manual/auto).
Snapshot capture works and writes to disk atomically.
**Restore is not implemented** — snapshot is a record, not a recovery path.

UI: read-only. No Apply button. `IStaticIpv4ApplyService` is registered in DI
but no ViewModel consumes it.

Profiles / settings persistence: not started.
CI: GitHub Actions, `windows-latest`, Release build + full test suite on push
to `main` and on PRs. Destructive tests stay skipped — no `IPMAN_*` in CI.
Elevation: whole app runs `requireAdministrator`. Helper split not started.
i18n: `Strings.resx` exists, 42 entries, single language.

# Next
1. Decide elevation architecture before writing Apply UI
2. Profile / settings persistence
3. Apply UI

# Constraints
- Destructive network tests are opt-in and must run only on an isolated VM adapter
- Never run network-mutating commands on the development machine
