# State

Network engine: working. Static IPv4, gateway, DNS (manual/auto).
Snapshot capture works and writes to disk atomically.
**Restore is not implemented** — snapshot is a record, not a recovery path.

UI: read-only. No Apply button. `IStaticIpv4ApplyService` and `IProfileCatalog`
are registered in DI but no ViewModel consumes them.

Profiles / settings: persistence done (TASK-001). One JSON file per profile under
`Documents\IPMan\Profiles\`, settings in `Documents\IPMan\settings.json`; recovery
snapshots stay in `%LocalAppData%\IPMan\Backup\` (ADR-015 supersedes ADR-012).
`ProfileCatalog` loads at startup, reloads on a debounced directory watch, and
isolates malformed files. Not displayed yet; DHCP profiles store but cannot apply.
CI: GitHub Actions, `windows-latest`, Release build + full test suite on push
to `main` and on PRs. Destructive tests stay skipped — no `IPMAN_*` in CI.
Elevation: settled — whole app runs `requireAdministrator`, helper split rejected
(ADR-013). Apply refuses to mutate when the process is not elevated.
Single instance: `Local\` mutex + admin-only named pipe activation. Mutation is
serialized machine-wide by a `Global\` mutex (ADR-011, ADR-014).
i18n: `Strings.resx` exists, 43 entries, single language.

# Next
1. Apply UI + profiles panel (load, search, favorites, import/export)
2. Tray + window state persistence (PR-019, PR-020, AC-018) — `AppSettings`
   takes the new fields additively, no schema bump needed

# Constraints
- Destructive network tests are opt-in and must run only on an isolated VM adapter
- Never run network-mutating commands on the development machine
