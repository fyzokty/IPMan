# State

Network engine: static IPv4, gateway, DNS (manual/auto), and DHCP with automatic DNS.
A snapshot is written before every mutation; restore reapplies the latest one for an
adapter through the two apply services, with per-adapter pruning (ADR-016). **The DHCP
and restore paths have never run against a real adapter** — only unit tests through the
fake WMI seam; real proof needs an isolated VM with the `IPMAN_*` opt-in.

UI: read-only, no Apply button. `IStaticIpv4ApplyService`, `IDhcpApplyService`,
`IRecoveryRestoreService` and `IProfileCatalog` are in DI but no ViewModel uses them.
Profiles / settings: one JSON file per profile under `Documents\IPMan\Profiles\`,
settings in `Documents\IPMan\settings.json`; recovery snapshots stay in
`%LocalAppData%\IPMan\Backup\` (ADR-015 supersedes ADR-012). `ProfileCatalog` loads
at startup, reloads on a debounced watch, and isolates malformed files.
CI: GitHub Actions, `windows-latest`, Release build + full suite on push to `main`
and on PRs. Destructive tests stay skipped — no `IPMAN_*` in CI.
Elevation: whole app runs `requireAdministrator`, helper split rejected (ADR-013).
Single instance: `Local\` mutex + admin-only pipe activation; mutation serialized
machine-wide by a `Global\` mutex (ADR-011, ADR-014).
i18n: `Strings.resx` exists, 43 entries, single language.

# Next
1. Apply UI: `Uygula`, DHCP and `Son Yapılandırmayı Geri Yükle` actions, plus the
   profiles panel as a fixed left pane (load, search, favorites, import/export)
2. Tray + window state persistence (PR-019, PR-020, AC-018) — `AppSettings` takes
   the new fields additively, no schema bump needed

# Constraints
- Destructive network tests are opt-in and must run only on an isolated VM adapter
- Never run network-mutating commands on the development machine
