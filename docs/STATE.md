# State

Network engine: static IPv4, gateway, DNS (manual/auto), and DHCP with automatic DNS.
A snapshot is written before every mutation; restore reapplies the latest one for an
adapter through the two apply services, with per-adapter pruning (ADR-016). **The DHCP
and restore paths have never run against a real adapter** — only unit tests through the
fake WMI seam; real proof needs an isolated VM with the `IPMAN_*` opt-in (backlog 11).

UI: the selected adapter tab applies. `Uygula`, `Otomatik Al (DHCP)` and
`Son Yapılandırmayı Geri Yükle` run through one shared `AdapterActionsViewModel`
whose `IsBusy` locks the edit fields; the draft validates live and shows per-field
Turkish errors once a field is edited. Conflict overrides and restore ask through
`IUserConfirmationService`; every outcome is inline text with a severity.
Profiles panel: fixed left pane over `IProfileCatalog` — favorites first, search,
context menu (rename, duplicate, favorite, export, delete), inline save, single-profile
JSON import/export, malformed files listed separately (backlog 01).
Shell: tray icon with close-to-tray choice asked once, persisted window bounds and last
adapter (02); settings window behind the gear (03); follow-Windows / light / dark theme
(04); quick actions — copy network info, gateway ping, DNS flush, IP renew (05);
Windows toast notifications for adapter-changing outcomes, switchable in settings (06);
critical-only log under `%LocalAppData%\IPMan\logs\` and an unclean-exit marker that
starts after the first change (07); a diagnostics window behind settings with a
copyable plain-text report (08).
Profiles / settings: one JSON file per profile under `Documents\IPMan\Profiles\`,
settings in `Documents\IPMan\settings.json`; recovery snapshots stay in
`%LocalAppData%\IPMan\Backup\` (ADR-015 supersedes ADR-012).
First startup silently captures visible adapters in `%LocalAppData%\IPMan\FirstRun\first-run.json`,
never overwriting it or exposing it to restore and retention (09, ADR-017).
CI: GitHub Actions, `windows-latest`, Release build + full suite on push to `main`
and on PRs. Destructive tests stay skipped — no `IPMAN_*` in CI.
Elevation: whole app runs `requireAdministrator`, helper split rejected (ADR-013).
Distribution: win-x64 self-contained portable zip and Inno Setup installer tooling added; tagged CI creates a draft Release, pending CI and clean Windows verification (10).
Single instance: `Local\` mutex + admin-only pipe activation; mutation serialized
machine-wide by a `Global\` mutex (ADR-011, ADR-014).
i18n: `Strings.resx`, single language (Turkish).

# Next
Order and details live in `docs/backlog/`:
1. 10 — portable package and installer
2. 11 — real-adapter verification on an isolated VM (can run in parallel)

# Constraints
- Destructive network tests are opt-in and must run only on an isolated VM adapter
- Never run network-mutating commands on the development machine
