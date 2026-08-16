# State

Network engine: working. Static IPv4, gateway, DNS (manual/auto).
Snapshot capture works and writes to disk atomically.
**Restore is not implemented** — snapshot is a record, not a recovery path.

UI: read-only. No Apply button. `IStaticIpv4ApplyService` is registered in DI
but no ViewModel consumes it.

Profiles / settings persistence: not started.
CI: not set up.
Elevation: whole app runs `requireAdministrator`. Helper split not started.
i18n: `Strings.resx` exists, 42 entries, single language.

# Next
1. CI (build + test on every PR)
2. Decide elevation architecture before writing Apply UI
3. Profile / settings persistence
4. Apply UI

# Constraints
- Destructive network tests are opt-in and must run only on an isolated VM adapter
- Never run network-mutating commands on the development machine
