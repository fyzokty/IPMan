# State

Network engine: static IPv4, gateway, DNS (manual/auto), and DHCP with automatic DNS.
A snapshot is written before every mutation; restore reapplies the latest one for an
adapter through the two apply services, with per-adapter pruning (ADR-016). **The DHCP
and restore paths have never run against a real adapter** — only unit tests through the
fake WMI seam; real proof needs an isolated VM with the `IPMAN_*` opt-in (backlog 11).

UI: Home/Details/ProfileEditor navigation uses a connection selector and saved-setting cards.
Details (16) loads the selected connection, shows live field differences and validation, and
applies static or automatic settings through the shared safe-switch coordinator. Cancel resets
the fields; save-as-setting prefills the new editor. Results and retry stay inline, unsaved
back/close prompts are shared, and losing the adapter returns Home and disposes the page.
Profiles: setting cards over `IProfileCatalog` — favorites first, search,
context menu (rename, duplicate, favorite, export, delete), full-page save, single-profile
JSON import/export, malformed files listed separately (backlog 01).
Shell: tray icon with close-to-tray choice asked once, persisted window bounds and last
adapter (02); settings window behind the gear (03); follow-Windows / light / dark theme
(04); quick actions — copy network info, gateway ping, DNS flush, IP renew (05);
Windows toast notifications for adapter-changing outcomes, switchable in settings (06);
critical-only log under `%LocalAppData%\IPMan\logs\` and an unclean-exit marker that
starts after the first change (07); a diagnostics window behind settings with a
copyable plain-text report (08).
Connections and details show Windows NLM/NCSI's IPv4 internet assessment,
without IPMan ping/HTTP/DNS probes, with plain Turkish connection names and virtual
adapters hidden by default (12). The main window is profile-centric (13): connection
selector, status card with wireless shortcut, Kart/Liste settings with search and a built-in
Otomatik card; "Bu ağa geç" applies directly. "Ayrıntılar ve elle ayar" opens the manual page.
Safe switching (14): cards, automatic settings, manual apply and DHCP use conditional
internet-loss confirmation and a mandatory 15-second topmost countdown. Keep, rollback
and rollback failure appear in the shared inline banner; notifications wait for the final
outcome. RecoveryRestoreService handles rollback, and real exit asks how to finish an
active countdown while hiding to the tray leaves it running. Restore itself starts no countdown.
Setting editor (15): new/edit open a full-page form in the main window's Home/Details/ProfileEditor navigation.
Save-only returns to the refreshed catalog; save-and-switch uses the shared safe-switch command for static and DHCP targets.
Suggestions exclude the gateway captured on opening; unsaved back/close prompts and adapter-busy switch gating are covered by fakes and light/dark host renders.
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
Design: Windows 11 Fluent tokens and simplified component specifications in `docs/03_UI_UX/DESIGN.md`, UX principles in `docs/03_UI_UX/UX-ilkeleri.md`, and 10 canonical reference screens in `docs/03_UI_UX/stitch-karma/` (ADR-018). The legacy technician layout in `docs/03_UI_UX/stitch/` is superseded. The main window follows the new design (13); 17–18 remain. Shared styles live in `Themes/Controls.xaml`.

# Next
Order and details live in `docs/backlog/`:
1. 10 — portable package and installer
2. 11 — real-adapter verification on an isolated VM (can run in parallel)
3. 17–18 — remaining simple-first UX overhaul (ADR-018: plain terminology, tray menu)

# Constraints
- Destructive network tests are opt-in and must run only on an isolated VM adapter
- Never run network-mutating commands on the development machine
