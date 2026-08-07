---
title: IPMan Product Requirements
version: 1.0.0
status: Approved
date: 2026-08-07
---

# 1. Purpose

This document defines what IPMan must provide to its users in release 1.0.

IPMan is an offline Windows 10/11 desktop utility for inspecting and changing
IPv4 network-adapter configuration without requiring manual CLI usage.

# 2. Target users

IPMan must be usable by:
- general Windows users,
- IT staff,
- network specialists,
- CCTV installers,
- PLC and automation technicians,
- field service personnel.

The product must not assume that every user understands Windows networking
administration.

# 3. Product experience

The application consists primarily of one resizable main window.

Visible network adapters are represented as tabs. Each tab owns its displayed
state and edit state. Actions taken while one tab is selected must not
accidentally modify another adapter.

The main workflow is:

1. Open IPMan.
2. See actual current Windows adapter configuration.
3. Select an adapter tab.
4. Optionally load a saved profile into the edit fields.
5. Review differences.
6. Apply static configuration or DHCP.
7. IPMan verifies Windows state after the operation.
8. User receives visible success/error feedback.
9. User can roll back the most recent configuration if needed.

# 4. Main-window requirements

The main window shall contain:

- adapter tabs,
- current-information panel,
- editable configuration panel,
- profiles panel,
- quick-actions area,
- settings access,
- status bar.

The initial layout shall use a two-panel information/editing concept while
retaining the profiles panel in the same main window.

# 5. Adapter tabs

Each adapter tab shall show:
- adapter icon,
- adapter display name,
- connection-status indicator.

Status shall use both icon and text where the selected adapter's detail is
shown.

Example concepts:
- green indicator + `Bağlı`,
- red indicator + `Bağlı Değil`.

Disconnected adapters remain editable.

Adapters added while IPMan runs shall appear automatically.

Adapters removed while IPMan runs shall disappear automatically and generate a
notification.

# 6. Current information panel

For the selected adapter, show:
- connection state,
- adapter name,
- adapter description,
- MAC address,
- IPv4 address,
- subnet mask,
- default gateway,
- primary DNS,
- secondary DNS,
- DHCP/static state,
- link speed when available.

Values shown as current configuration must come from Windows, not from stale UI
state or the last requested configuration.

# 7. Editable configuration panel

Provide one text box for each:
- IPv4,
- subnet mask,
- gateway,
- primary DNS,
- secondary DNS.

Each field shall have a copy action.

Gateway may be empty.

DNS values are optional and both may be empty.

Provide `Mevcut Değeri Getir` to repopulate editing fields from actual current
Windows state.

Primary action: `Uygula`.

Also provide DHCP and profile-save actions in the same working area.

# 8. Validation

Validate values as the user edits them.

Required static values:
- IPv4,
- subnet mask.

Optional:
- gateway,
- primary DNS,
- secondary DNS.

Invalid values shall not be applied.

Feedback must identify the invalid field in user-friendly Turkish.

# 9. Applying static settings

Application flow:

1. Validate edit fields.
2. Compare desired and current state.
3. If equivalent, do not make a Windows configuration call.
4. Capture rollback state.
5. Run best-effort duplicate-IP/conflict detection.
6. If conflict is suspected, warn that the IP *may* be in use.
7. Permit explicit user override.
8. Apply asynchronously.
9. Reread state from Windows.
10. Verify result.
11. Refresh selected tab.
12. Update status bar.
13. Show Windows-style notification.

Controls that could cause conflicting changes shall be disabled while applying.

# 10. DHCP

Provide an action to use automatic IP configuration.

DHCP action shall also return DNS to automatic configuration.

After DHCP is requested, IPMan shall reread Windows state and verify the result.

DHCP itself is a valid saveable profile configuration.

# 11. Rollback

Before every network modification, capture the current selected-adapter
configuration.

Expose a one-click `Son Yapılandırmayı Geri Yükle` capability.

Rollback is adapter-specific to the snapshot that was captured.

If an apply attempt fails, retain rollback information.

# 12. Profiles

Profiles are reusable configurations stored as standalone JSON files.

Profile fields:
- profile name,
- optional description,
- mode: DHCP or static,
- IPv4,
- mask,
- gateway,
- primary DNS,
- secondary DNS,
- favorite flag,
- schema version,
- created/modified metadata.

Profiles are portable between computers and shall not be hard-bound to a
specific physical adapter.

Profile-origin adapter information may be retained as optional context. If
applying to a different adapter, warn but allow.

# 13. Profile selection

Default behavior:
- select profile,
- fill edit controls,
- do not apply.

A persistent setting shall allow:
- apply immediately when a profile is selected.

Even when automatic apply is enabled, normal validation, backup, conflict
warning and verification rules still apply.

# 14. Profile naming

Saving an existing profile name shall not silently overwrite.

Generate next available display name:
- `PLC`
- `PLC (1)`
- `PLC (2)`

# 15. Profile panel

Profile panel remains visible in main window.

It shall include:
- search,
- favorites group,
- other profiles group,
- right-click context menu.

Favorites are shown first.

Profiles are alphabetical within their group.

Search covers at minimum:
- profile name,
- description,
- IP values.

Profile context menu shall include appropriate actions such as:
- load,
- rename/edit,
- duplicate,
- favorite/unfavorite,
- export,
- delete,
- open file location.

Destructive delete requires confirmation.

# 16. Import/export

One profile may be exported as a JSON file.

One profile JSON file may be imported.

Multi-profile ZIP export is not release-1.0 scope.

# 17. Corrupt profiles

One invalid JSON profile must not stop application startup or prevent healthy
profiles from loading.

Invalid profile files shall remain preserved.

The UI shall surface problematic profiles clearly in a problem state/group and
allow actions such as delete or open file location.

# 18. External profile changes

The Profiles directory shall be monitored.

External add/remove/change operations shall refresh the profile list.

Repeated filesystem events must be debounced.

# 19. Profile comparison

When a profile is selected, IPMan shall make it clear which relevant values
differ from the selected adapter's current Windows configuration.

Different values should be visually emphasized before application.

# 20. Settings

Settings are opened from a small gear icon as a dialog or small settings window.

Initial settings include:
- theme: follow Windows / light / dark,
- notifications enabled,
- close behavior,
- system tray behavior,
- profile auto-apply behavior,
- relevant adapter visibility options,
- stored window state.

# 21. Window behavior

Main window is resizable and has a sensible minimum size.

Persist:
- width/height,
- position,
- last selected adapter where still available.

Restoring from the tray restores prior window state.

# 22. System tray

IPMan supports the Windows notification area.

When the close/tray choice is first relevant, the application captures the
user's preference and persists it.

Subsequent behavior follows the saved preference.

# 23. Single instance

Launching IPMan while it is already running shall not create a second
independent network-management window.

The existing instance shall be restored/brought to foreground.

# 24. Status bar

Status bar shows contextual information such as:
- connection state,
- administrator state,
- last refresh time,
- application version,
- operation state.

Operation examples:
- `Hazır`,
- `Uygulanıyor...`,
- `Hata`.

# 25. Quick actions

Release 1.0 quick actions include:
- DHCP,
- copy network information,
- ping gateway,
- flush DNS,
- renew IP where applicable.

Actions must respect selected-adapter scope where applicable.

# 26. Notifications

Use Windows 11-style notification presentation for important outcomes.

Critical operation state shall also remain visible in-app; a transient
notification must not be the sole indicator of failure.

# 27. Startup behavior

Window appears promptly.

Sections may initially show `Yükleniyor...` while asynchronous discovery
completes.

Startup sequence includes:
- load settings,
- load profiles,
- discover adapters,
- create/update adapter tabs,
- read actual configurations,
- update UI.

Do not auto-apply a profile at startup.

# 28. Initial local data

If configuration directories/files are missing, create them automatically.

Do not interrupt first launch merely to ask the user to create a profile.

Capture recoverable initial network state automatically.

# 29. Logging and crash recovery

Log critical technical failures only.

Examples:
- unreadable JSON,
- write failures,
- Windows API failures,
- unhandled exceptions.

On next start after unexpected termination, show a brief informative message.

# 30. Diagnostics

Provide a diagnostics panel that can show:
- Windows version,
- IPMan version,
- administrator status,
- adapter count,
- profile count,
- profile/config storage status.

Allow copy-to-clipboard.

# 31. Localization

Release 1.0 UI is Turkish.

All user-facing strings must use localization infrastructure from first
implementation.

# 32. Distribution

Provide:
- portable distribution,
- installer distribution.

No automatic Internet update mechanism in release 1.0.

# 33. Privacy/offline

IPMan core functionality requires no Internet connection.

Do not transmit profiles or network configuration externally.
