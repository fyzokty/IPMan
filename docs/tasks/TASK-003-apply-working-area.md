# TASK-003: Apply working area (Uygula, DHCP, restore)

- **Owner agent:** codex-coder
- **Status:** done
- **Depends on:** TASK-002

## Goal
Connect the finished network engine to the user. `IStaticIpv4ApplyService`,
`IDhcpApplyService` and `IRecoveryRestoreService` are registered in DI but no
ViewModel calls them, so the window is still read-only. After this task the
selected adapter tab offers `Uygula`, `Otomatik Al (DHCP)` and
`Son Yapılandırmayı Geri Yükle`, validates the draft as the user types, asks for
confirmation where the engine demands it, and reports every outcome in Turkish
without a modal error dialog (`docs/01_Product_Requirements.md` sections 7-11).

## Architecture decisions
- **One actions ViewModel, not one per tab.** `AdapterActionsViewModel` is a
  single DI-registered instance owned by `MainWindowViewModel` and attached to
  the selected adapter. PR section 9 requires conflicting controls to be
  disabled during an apply; one instance gives one `IsBusy` flag without a
  second shared-gate concept. Machine-wide serialization already lives in
  `CrossProcessNetworkMutationCoordinator`.
- **Live validation lives in the draft.** Per-field errors are per-tab state
  that must react to every keystroke, so `AdapterDraftViewModel` takes
  `IStaticIpv4ConfigurationValidator`. A field's error becomes visible only
  after that field has been edited, or after an apply attempt, so a freshly
  opened window is never red.
- **`Uygula` is always clickable.** Validity is checked on execute, not in
  `CanExecute`, so an invalid draft explains itself instead of leaving a dead
  button.
- **Confirmation is a seam, not a MessageBox call in a ViewModel.**
  `IUserConfirmationService` keeps the conflict-override and restore prompts
  testable; the WPF implementation is the only place that touches `MessageBox`.
- **No modal on failure.** Every outcome is inline text with a severity, matching
  the existing inline refresh-failure rule.

## Scope
Delivered in two sequential codex-coder runs:
- **A — ViewModel layer:** actions ViewModel, draft validation, message mapping,
  confirmation seam, strings, DI, tests. No XAML.
- **B — View layer:** `MainWindow.xaml` action row, inline validation display,
  busy state, result banner.

## Acceptance criteria
- [x] `Uygula` validates first; an invalid draft names the offending fields in
      Turkish and makes no Windows call
- [x] An unchanged draft reports the no-change outcome and makes no Windows call
- [x] `ConflictConfirmationRequired` and `ProbeIndeterminateConfirmationRequired`
      produce an explicit prompt; declining leaves Windows untouched, accepting
      retries once with the matching flag set
- [x] `Otomatik Al (DHCP)` runs the DHCP path and reports its verified outcome
- [x] `Son Yapılandırmayı Geri Yükle` confirms before mutating and reports
      `NoSnapshotFound` as an ordinary outcome, not an error dialog
- [x] Every member of `StaticIpv4ApplyStatus`, `DhcpApplyStatus`,
      `RecoveryRestoreStatus` and `StaticIpv4SafetyBlock` maps to user-facing
      Turkish text; no status falls through to a technical enum name
- [x] All three actions are disabled while one of them runs
- [x] A completed action requests an adapter refresh
- [x] No user-facing literal outside `Strings.resx`
- [x] No `MessageBox` reference outside the confirmation implementation

## Verification
```
dotnet build
dotnet test
```
No destructive network test runs. The apply services are exercised through fakes.

## Out of scope
Profiles panel and profile save/load (next task), tray, notifications, window
state persistence, IPv6, settings screen.
