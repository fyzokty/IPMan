# TASK-001: Profile and settings persistence core

- **Owner agent:** codex-coder
- **Status:** done
- **Depends on:** none

## Goal
IPMan stores named, adapter-independent network configurations and application
settings as plain JSON files under `%UserProfile%\Documents\IPMan\`, loads them at
startup, and refreshes itself when the profiles directory changes on disk. No
database, no ORM, no new NuGet package. This task delivers the persistence core
only; nothing is displayed in the user interface yet.

## Scope
Paths this task may modify:
- `src/IPMan.Domain/Profiles/**`, `src/IPMan.Domain/Settings/**`
- `src/IPMan.Application/Profiles/**`, `src/IPMan.Application/Settings/**`
- `src/IPMan.Infrastructure/Common/**`, `src/IPMan.Infrastructure/Profiles/**`,
  `src/IPMan.Infrastructure/Settings/**`
- `src/IPMan.Infrastructure/Networking/JsonRecoverySnapshotRepository.cs` (refactor only)
- `src/IPMan.App/App.xaml.cs` (DI registration and startup wiring only)
- `tests/IPMan.Tests/Profiles/**`, `tests/IPMan.Tests/Settings/**`,
  `tests/IPMan.Tests/Fakes/**`, `tests/IPMan.Tests/TestData.cs`
- `tests/IPMan.IntegrationTests/Harness/ProductionHarnessFactory.cs` (path dedup only)

Paths this task must NOT touch:
- everything else, in particular `docs/contracts/**`, `src/IPMan.App/ViewModels/**`,
  `src/IPMan.App/Views/**`, `src/IPMan.App/Resources/**`, `src/IPMan.Application/Networking/**`,
  any `*.csproj`, `Directory.*.props`, `.github/**`

## Acceptance criteria
- [ ] Profiles are written one JSON file per profile under `Documents\IPMan\Profiles\`
- [ ] `settings.json` is written under `Documents\IPMan\`
- [ ] Recovery snapshots still write to `%LocalAppData%\IPMan\Backup\`; existing
      `JsonRecoverySnapshotRepositoryTests.cs` passes without a single line changed
- [ ] Profile identity comes from the JSON `profileId`, never from the file name
- [ ] Saving an existing profile name produces `PLC (1)`, not an overwrite (BR-012, AC-011)
- [ ] One malformed profile JSON is preserved on disk, reported as a problem, and does
      not prevent healthy profiles from loading (FR-PRO-014, AC-014)
- [ ] External changes to the profiles directory raise one debounced refresh (FR-PRO-015, AC-015)
- [ ] Profile ordering puts favorites first, alphabetical within group (AC-012)
- [ ] Profile search matches name, description and IP values (AC-013)
- [ ] Enums in profile and settings JSON are readable strings, not integers
- [ ] All writes are atomic; no `*.tmp` file survives a successful write
- [ ] Repository methods return typed results and never throw for IO or access failures
- [ ] No new NuGet package; no `IFileSystem` abstraction
- [ ] Existing 465 unit tests still pass

## Verification
```
dotnet build
dotnet test
```
Plus: launch the app once, confirm `Documents\IPMan\Profiles\` and `settings.json`
are created, and that a hand-placed malformed JSON does not break startup.

## Out of scope
No profile panel, no search box, no favorites menu, no import/export dialogs, no
draft population on profile selection, no auto-apply wiring, no window/tray settings
fields, no theme application, no DHCP mutation path. `Strings.resx` is not touched —
this task adds no user-visible text.

## Notes
Read first: `docs/ADR/ADR-003-JSON-Persistence.md`,
`docs/ADR/ADR-015-User-Documents-Storage.md`,
`docs/02_Architecture/06_Persistence_Architecture.md`,
`docs/01_Product_Requirements.md` sections 12-18 and 20,
`src/IPMan.Infrastructure/Networking/JsonRecoverySnapshotRepository.cs` and
`RecoverySnapshotJsonCodec.cs` for the house persistence pattern,
`src/IPMan.Application/Networking/AdapterRefreshCoordinator.cs` for the
event/lifecycle pattern.

Delivered in two sequential codex-coder runs:
A = storage layout, atomic writer, domain types, codecs, repositories.
B = watcher, catalog, ordering/search, DI and startup wiring.
