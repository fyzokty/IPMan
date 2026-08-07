---
title: IPMan Acceptance Criteria
version: 1.0.0
status: Approved
---

# Release 1.0 Acceptance Criteria

## AC-001 — Startup state
Given IPMan starts on a supported Windows machine,
when the main window becomes ready,
then it shows adapter tabs and current values read from Windows,
and it does not auto-apply a saved profile.

## AC-002 — Adapter isolation
Given two adapters exist,
when the user edits and applies settings on adapter A,
then adapter B is not modified by that action.

## AC-003 — Disconnected adapter
Given an adapter is disconnected,
when the user selects it,
then its configuration is viewable/editable and the UI clearly shows
`Bağlı Değil`.

## AC-004 — Validation
Given an invalid IPv4 value,
when the user attempts Apply,
then Windows configuration is not changed and field feedback is shown.

## AC-005 — Optional DNS
Given valid static IPv4/mask and blank DNS fields,
when the user applies,
then blank DNS alone is not treated as validation failure.

## AC-006 — No-op apply
Given desired state equals current state,
when the user chooses Apply,
then no Windows mutation is performed and the user receives an informational
result.

## AC-007 — Verification
Given a network change is requested,
when the operation finishes,
then success is reported only after IPMan rereads Windows state and verifies the
result.

## AC-008 — Rollback
Given a successful configuration change was made,
when the user invokes restore-last-configuration,
then IPMan attempts to restore the captured previous state and verifies it.

## AC-009 — Profile default load
Given profile auto-apply is disabled,
when the user selects a profile,
then edit fields change but Windows configuration does not.

## AC-010 — Profile auto-apply
Given profile auto-apply is enabled,
when a valid profile is selected,
then standard validation/backup/apply/verify workflow runs.

## AC-011 — Duplicate profile
Given `PLC` exists,
when another `PLC` is saved,
then the original remains intact and the new profile receives the next available
numbered name.

## AC-012 — Favorites
Given favorite and non-favorite profiles exist,
when the list is shown,
then favorites appear first and each group is alphabetically ordered.

## AC-013 — Search
Given profiles contain matching name, description or IP text,
when the user searches that text,
then matching profiles remain visible.

## AC-014 — Corrupt profile
Given one profile JSON is malformed,
when IPMan loads profiles,
then healthy profiles load, the bad file is preserved/surfaced and the
application remains usable.

## AC-015 — External file change
Given IPMan is running,
when a valid profile JSON is added externally,
then the profile list updates without requiring application restart.

## AC-016 — Adapter insertion/removal
Given IPMan is running,
when an adapter is added or removed,
then tabs update automatically and removal produces visible feedback.

## AC-017 — Single instance
Given IPMan is already running,
when it is launched again,
then the original window is brought forward and no second interactive instance
continues running.

## AC-018 — Tray restore
Given IPMan is hidden in the tray,
when the user restores it,
then prior valid window size and position are restored.

## AC-019 — Localization
Given release 1.0,
when the UI is rendered,
then visible product strings are Turkish and sourced through localization
resources rather than embedded business logic.

## AC-020 — Offline use
Given Internet access is unavailable,
when the user performs core adapter/profile operations,
then IPMan remains functional.

## AC-021 — Diagnostics
Given the diagnostics panel is opened,
then support information can be copied to the clipboard.

## AC-022 — Critical log
Given a critical JSON/API/unhandled error occurs,
then a troubleshooting log entry is produced without creating routine user
activity history.
