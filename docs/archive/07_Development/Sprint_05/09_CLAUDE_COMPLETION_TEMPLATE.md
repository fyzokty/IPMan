# Sprint 05 Completion Report

## Build

- Debug: `dotnet build IPMan.sln` — succeeded
- Release: `dotnet build IPMan.sln -c Release` — succeeded
- Warning count: 0
- Error count: 0

Environment: Windows 11 x64, .NET SDK 8.0.423 (pinned by `global.json`). No
analyzer rule was suppressed; the only pre-existing relaxation remains `CA1707`
in the test project, approved in Sprint 04 for the mandated test naming style.

## Tests

- Total: 156
- Passed: 156
- Failed: 0
- Skipped: 0

62 new tests were added; all 94 Sprint 04 tests still pass. `dotnet test` was run
in Debug and Release, three consecutive Release runs, with identical results.
Every test is read-only with respect to Windows network configuration.

## Git change summary

`git diff --stat` against the Sprint 04 baseline (`4162b8e`), including new files:

```
 src/IPMan.App/App.xaml                             |  67 +++-
 src/IPMan.App/App.xaml.cs                          |   3 +
 .../Presentation/AdapterDisplayFormatter.cs        | 109 +++++
 .../Presentation/IApplicationVersionProvider.cs    |  37 ++
 src/IPMan.App/Presentation/IClipboardService.cs    |  14 +
 src/IPMan.App/Presentation/WpfClipboardService.cs  |  17 +
 src/IPMan.App/Resources/Strings.cs                 |  89 ++++-
 src/IPMan.App/Resources/Strings.resx               | 125 ++++--
 .../ViewModels/AdapterDetailsViewModel.cs          |  94 +++++
 src/IPMan.App/ViewModels/AdapterDraftViewModel.cs  | 131 ++++++
 src/IPMan.App/ViewModels/AdapterViewModel.cs       |  76 ++++
 src/IPMan.App/ViewModels/DraftField.cs             |  11 +
 src/IPMan.App/ViewModels/DraftValues.cs            |  34 ++
 src/IPMan.App/ViewModels/MainWindowViewModel.cs    | 188 ++++++++-
 src/IPMan.App/ViewModels/StatusBarViewModel.cs     |  25 ++
 src/IPMan.App/Views/MainWindow.xaml                | 442 ++++++++++++++++++---
 .../Common/IElevationStateProvider.cs              |  10 +
 .../Common/WindowsElevationStateProvider.cs        |  21 +
 .../Fakes/FakeAdapterRefreshCoordinator.cs         |  67 ++++
 tests/IPMan.Tests/Fakes/FakeClipboardService.cs    |  12 +
 tests/IPMan.Tests/Fakes/FakeEnvironment.cs         |  25 ++
 tests/IPMan.Tests/Fakes/FakeUiDispatcher.cs        |  20 +
 tests/IPMan.Tests/IPMan.Tests.csproj               |   3 +
 .../Presentation/AdapterDisplayFormatterTests.cs   | 130 ++++++
 tests/IPMan.Tests/TestData.cs                      |  26 +-
 .../ViewModels/AdapterDraftViewModelTests.cs       | 184 +++++++++
 .../ViewModels/MainWindowViewModelTests.cs         | 386 ++++++++++++++++++
 27 files changed, 2216 insertions(+), 130 deletions(-)
```

The stat above omits one deletion that `git diff --stat` shows separately:
`src/IPMan.App/ViewModels/AdapterDiagnosticItem.cs` (106 lines) — the temporary
Sprint 04 diagnostic projection, replaced by the production ViewModels.

The change set contains Sprint 05 only. No Sprint 04 production behaviour was
altered; `IPMan.Domain` was not touched at all.

## Files changed

### Added — `IPMan.Application`

| File | Purpose |
|---|---|
| `Common/IElevationStateProvider.cs` | Contract for reporting process elevation. |

### Added — `IPMan.Infrastructure`

| File | Purpose |
|---|---|
| `Common/WindowsElevationStateProvider.cs` | Reads the Windows process token once via `WindowsPrincipal`. |

### Added — `IPMan.App`

| File | Purpose |
|---|---|
| `Presentation/IClipboardService.cs` | Clipboard abstraction keeping static OS calls out of ViewModels. |
| `Presentation/WpfClipboardService.cs` | WPF implementation; empty text clears instead of throwing. |
| `Presentation/IApplicationVersionProvider.cs` | Version contract plus the assembly-based implementation. |
| `Presentation/AdapterDisplayFormatter.cs` | Deterministic, testable formatting (placeholder, connection state, mode, link speed, additional addresses, refresh time). |
| `ViewModels/AdapterViewModel.cs` | One adapter tab, keyed by Windows identity; owns its details and draft. |
| `ViewModels/AdapterDetailsViewModel.cs` | Read-only projection of the latest snapshot. |
| `ViewModels/AdapterDraftViewModel.cs` | Editable draft with dirty state, copy and `Mevcut Değeri Getir`. |
| `ViewModels/DraftValues.cs` | Immutable five-field value set used as the dirty-state baseline. |
| `ViewModels/DraftField.cs` | Field selector for the copy command. |
| `ViewModels/StatusBarViewModel.cs` | Status bar state holder. |

### Added — tests

| File | Purpose |
|---|---|
| `Fakes/FakeAdapterRefreshCoordinator.cs` | Publishes discovery results and failures on demand. |
| `Fakes/FakeUiDispatcher.cs` | Runs posted work inline and counts posts. |
| `Fakes/FakeClipboardService.cs` | Records copied text. |
| `Fakes/FakeEnvironment.cs` | Fake clock, elevation state and version providers. |
| `Presentation/AdapterDisplayFormatterTests.cs` | Formatting rules. |
| `ViewModels/AdapterDraftViewModelTests.cs` | Draft, dirty-state, reset and copy behaviour. |
| `ViewModels/MainWindowViewModelTests.cs` | Selection, merge, states and status bar. |

### Modified

| File | Change |
|---|---|
| `src/IPMan.App/Views/MainWindow.xaml` | Replaced the diagnostic grid with the production layout. |
| `src/IPMan.App/App.xaml` | Shared brushes, card/label/field/status styles and the visibility converter. |
| `src/IPMan.App/App.xaml.cs` | Registers elevation, clipboard and version services. |
| `src/IPMan.App/ViewModels/MainWindowViewModel.cs` | Rewritten as the tab/selection/state coordinator. |
| `src/IPMan.App/Resources/Strings.resx`, `Strings.cs` | All Sprint 05 user-facing text; the unavailable placeholder is now `—`. |
| `tests/IPMan.Tests/IPMan.Tests.csproj` | References `IPMan.App` and enables `UseWPF` for ViewModel tests. |
| `tests/IPMan.Tests/TestData.cs` | Snapshot builder accepts the fields the UI tests vary. |

### Removed

| File | Reason |
|---|---|
| `src/IPMan.App/ViewModels/AdapterDiagnosticItem.cs` | Temporary Sprint 04 diagnostic projection, superseded. |

## Behavior implemented

- Production main window: header, adapter tab strip, two-panel content, bottom
  status bar. Resizable, minimum size unchanged (900×600), no animation, no
  third-party UI package.
- Adapter tabs for every discoverable non-loopback adapter, each showing the name
  and a connection marker whose shape differs (`●` / `○`) as well as its colour,
  with `AutomationProperties.Name` carrying "name — Bağlı/Bağlı Değil".
- Current configuration panel: connection state, name, description, MAC,
  configuration mode, IPv4, mask, gateway, primary and secondary DNS, link speed
  and — when present — the additional IPv4 addresses.
- Draft panel with IPv4, mask, gateway, primary DNS and secondary DNS text
  fields, a per-field `Kopyala` action and `Mevcut Değeri Getir`.
- Status bar: application state (`Hazır` / `Yükleniyor...` / `Hata`), selected
  adapter and its connection state, last successful refresh time, administrator
  state and application version.
- Loading, empty and refresh-failure presentation states.
- All text resolved from `Strings.resx` (Turkish).

## Architecture compliance

- **No network mutation**: no command added in this sprint writes IP, mask,
  gateway, DNS or DHCP state. `INetworkAdapterConfigurator` is still
  unimplemented, and no Apply/DHCP button exists — not even a disabled one.
- **Adapter identity based selection**: tabs and selection are keyed by
  `NetworkAdapterId`; index and display name are never used for state. A renamed
  or reordered adapter keeps its tab, its details and its draft.
- **Snapshot/draft separation**: `NetworkAdapterSnapshot` is never mutated. The
  draft is separate presentation state with its own dirty flag, and a test
  asserts the snapshot is unchanged after editing.
- **UI dispatcher boundary**: every coordinator event is marshalled through
  `IUiDispatcher`. No WPF `Dispatcher` use exists in Domain, Application or
  Infrastructure, and no ViewModel calls a Windows networking API.
- **Localization**: every new user-facing string comes from `Strings.resx`.
- **Domain untouched**: `IPMan.Domain` has no Sprint 05 changes.

## UI behavior

- **Adapter tabs** — the tab collection is reconciled in place on each refresh:
  known adapters are updated and moved to their discovery position, new adapters
  are inserted, disappeared adapters are removed. Existing tab ViewModels are
  reused, which is what keeps drafts alive.
- **Selected adapter** — if the previously selected identity still exists it stays
  selected; otherwise the first adapter in discovery order is selected; if there
  are none, selection is cleared and the empty state appears.
- **Draft retention** — each adapter owns its draft, so edits to one adapter never
  affect another and switching tabs applies nothing. Drafts live as long as the
  adapter does and are not persisted across restarts.
- **Refresh interaction** — the current panel always follows Windows. A clean
  draft is synchronized with the new snapshot; a dirty draft is left untouched.
  `Mevcut Değeri Getir` is the only action that discards edits, resetting the
  selected draft to the latest snapshot and clearing dirty state.
- **Loading** — shown until the first pass completes or fails; discovery runs on a
  background thread, so the window stays responsive.
- **Empty** — shown when discovery succeeded and Windows reported no adapters.
- **Refresh failure** — an inline warning bar appears above the tabs, the status
  bar switches to `Hata`, and the last valid adapter state stays on screen. No
  modal dialog, no blanked window. A later successful pass clears it.
- **Status bar** — fixed to the bottom, single line.

Verified by rendering the real window off-screen with stub services: loading,
populated, refresh-error and empty states all lay out at the 900×600 minimum and
at 1400×900, with **zero WPF binding errors** and mutually exclusive state
visibility.

## Tests added

- Formatting: unavailable placeholder (never `0.0.0.0`), connection state text,
  the three configuration modes, link speed (`1 Gbps`, `2.5 Gbps`, `100 Mbps`,
  `121.5 Mbps`, `56 Kbps`, unavailable), additional IPv4 presentation, refresh
  time in local time.
- Draft: initialization from the snapshot, empty text for absent optional values,
  dirty on edit, clean again when edited back, clean drafts follow refreshes,
  dirty drafts survive refreshes, `Mevcut Değeri Getir` resets values and dirty
  state, the domain snapshot is never modified.
- Copy: exact draft text per field, every field supported, empty value copies
  empty text without failing, copying changes no other state.
- Selection: deterministic first selection, preservation across refresh including
  rename and reorder, tab ViewModel reuse, new adapters appear without restart,
  fallback selection on removal, cleared selection with empty state.
- States: loading before the first result, content/empty/error visibility,
  failure keeps the last valid state, success clears the error, status bar
  content, dispatcher marshalling, disposal stops observation.

## New dependencies

`None`

`UseWPF` was enabled in the test project so it can reference `IPMan.App` for
ViewModel tests; that is an SDK feature, not a package.

## Known limitations

1. The status bar shows elevation state but the UI does not yet explain what
   cannot be done without it — there is nothing to disable while the sprint has
   no mutation actions.
2. Adapter configurability is still not modelled (architect Decision 1), so the
   UI cannot yet state why a particular adapter may not be editable.
3. Additional IPv4 addresses are informational and read-only, as specified; the
   draft continues to target the single primary address.
4. Drafts are in-memory only and are discarded when an adapter disappears or the
   application exits. No persistence was required.
5. The window has no settings/gear affordance. The layout leaves room for it, but
   a non-functional control was deliberately not added.
6. `MainWindow.xaml` itself has no automated coverage; its correctness was
   verified by off-screen rendering with binding-error capture rather than by UI
   automation tests, which the sprint explicitly does not want.
7. Colours are currently fixed light-theme brushes in `App.xaml`. Theme support
   (Follow Windows / Light / Dark) remains future work.

## Questions for architect

1. Theme support is listed in `CLAUDE.md` but not in the Sprint 05 scope. The
   brushes are already centralized in `App.xaml`, so swapping them out later is
   cheap — should theming be scheduled before or after the mutation sprint?
2. When mutation arrives, should a dirty draft block or warn on adapter removal
   and application exit? Sprint 05 discards such drafts silently, which is
   harmless while nothing can be applied.

## Do not continue

Sprint 06 has not been started. Awaiting architect review.
