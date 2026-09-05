# State

Network engine: static IPv4, gateway, DNS (manual/auto), and DHCP with automatic DNS.
A snapshot is written before every mutation; restore reapplies the latest one for an
adapter through the two apply services, with per-adapter pruning (ADR-016). **The DHCP
and restore paths have never run against a real adapter** — only unit tests through the
fake WMI seam; real proof needs an isolated VM with the `IPMAN_*` opt-in.

UI: the selected adapter tab applies. `Uygula`, `Otomatik Al (DHCP)` and
`Son Yapılandırmayı Geri Yükle` run through one shared `AdapterActionsViewModel`
whose `IsBusy` locks the edit fields; the draft validates live and shows per-field
Turkish errors once a field is edited. Conflict overrides and restore ask through
`IUserConfirmationService`; every outcome is inline text with a severity, never a
modal. `IProfileCatalog` is still in DI with no ViewModel behind it.
Profiles / settings: one JSON file per profile under `Documents\IPMan\Profiles\`,
settings in `Documents\IPMan\settings.json`; recovery snapshots stay in
`%LocalAppData%\IPMan\Backup\` (ADR-015 supersedes ADR-012). `ProfileCatalog` loads
at startup, reloads on a debounced watch, and isolates malformed files.
CI: GitHub Actions, `windows-latest`, Release build + full suite on push to `main`
and on PRs. Destructive tests stay skipped — no `IPMAN_*` in CI.
Elevation: whole app runs `requireAdministrator`, helper split rejected (ADR-013).
Single instance: `Local\` mutex + admin-only pipe activation; mutation serialized
machine-wide by a `Global\` mutex (ADR-011, ADR-014).
i18n: `Strings.resx` exists, 89 entries, single language.

# Next
1. Profiles panel as a fixed left pane (load, search, favorites, context menu,
   import/export) plus a profile-save action in the working area
2. Tray + window state persistence (PR-019, PR-020, AC-018) — `AppSettings` takes
   the new fields additively, no schema bump needed

# Constraints
- Destructive network tests are opt-in and must run only on an isolated VM adapter
- Never run network-mutating commands on the development machine
