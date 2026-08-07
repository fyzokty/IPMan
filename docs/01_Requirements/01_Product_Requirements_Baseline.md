---
title: IPMan Product Requirements Baseline
version: 1.0.0
status: Approved Baseline
---

# Product Requirements Baseline

This document captures product decisions already approved for the first
implementation. Detailed SRS documents may expand these requirements but must not
contradict them without an explicit product decision.

## PR-001 — Multi-adapter management

IPMan shall discover and manage Windows network adapters and display them in a
tabbed interface.

Each adapter tab shall:
- identify the adapter,
- show connection status using both icon and text,
- display its current network data,
- provide editing controls that act only on that selected adapter.

Adapters may include physical, USB and virtual adapters exposed by Windows.

## PR-002 — Adapter lifecycle

When adapters are added or removed while IPMan is running, the UI shall update
automatically.

A removed adapter shall disappear from the tab list and the user shall receive a
Windows-style notification.

The preferred mechanism is event-driven Windows network change observation.
Fallback reconciliation may be used if needed for reliability.

## PR-003 — Current configuration display

On startup IPMan shall read actual current Windows settings rather than loading
a saved profile as current state.

The information panel shall include:
- connected/disconnected state,
- adapter name,
- adapter description,
- MAC address,
- IPv4 address,
- subnet mask,
- default gateway,
- primary DNS,
- secondary DNS,
- DHCP/static state,
- link speed where available.

Disconnected adapters remain configurable.

## PR-004 — Editable configuration

The selected adapter shall provide text boxes for:
- IPv4,
- subnet mask,
- gateway,
- primary DNS,
- secondary DNS.

Each field shall provide a copy icon.

Gateway may be empty.

Both DNS fields may be empty.

A `Get Current Values` action shall restore the currently-read Windows values
into the edit fields.

## PR-005 — Validation

IPv4-related fields shall validate while the user edits them.

Invalid required values must prevent application and show understandable
feedback.

Validation should use appropriate .NET networking types/logic rather than relying
only on regular expressions.

## PR-006 — Applying static configuration

When the user chooses Apply:
1. validate values,
2. detect whether requested configuration already equals current configuration,
3. if equal, do not invoke Windows modification APIs,
4. capture a rollback snapshot,
5. perform a best-effort IP conflict check,
6. warn if the requested IP may already be in use,
7. allow the user to continue despite that warning,
8. apply the change asynchronously,
9. reread actual Windows settings,
10. verify the result,
11. refresh UI,
12. show a Windows-style success/failure notification.

During application, relevant controls are disabled and status displays
`Uygulanıyor...`; the main window must remain responsive.

## PR-007 — DHCP

The user shall be able to switch the selected adapter to automatic addressing.

The DHCP operation shall make IP addressing automatic and DNS automatic.

Afterward IPMan shall reread and verify Windows state.

DHCP configurations may also be saved as profiles.

## PR-008 — Rollback

Immediately before a network change, IPMan shall capture the selected adapter's
existing configuration.

The user shall be able to restore the most recent configuration.

If a change operation fails partially, IPMan shall retain the rollback data and
offer recovery where technically safe.

## PR-009 — Profiles

Profiles shall be stored as separate JSON files.

A profile shall contain:
- profile name,
- optional description,
- DHCP/static mode,
- IPv4 when applicable,
- subnet mask when applicable,
- gateway when applicable,
- primary DNS when applicable,
- secondary DNS when applicable,
- favorite flag,
- schema/version metadata.

Profiles shall not be permanently bound to one physical adapter.

If profile-origin adapter metadata is retained for context, applying the profile
to a different adapter shall warn the user rather than block the operation.

## PR-010 — Profile behavior

Default behavior:
- selecting a profile fills the editing controls,
- it does not immediately modify Windows.

Settings shall include:
- `Apply profile immediately when selected`.

When enabled, profile selection may immediately start the normal validated apply
workflow.

## PR-011 — Duplicate profile names

Saving a profile with a name already in use shall not overwrite the existing
profile automatically.

IPMan shall generate the next available numbered name, for example:
- `PLC`
- `PLC (1)`
- `PLC (2)`

## PR-012 — Profile organization

Profile panel shall:
- remain visible in the main window,
- include search,
- show favorites before other profiles,
- sort alphabetically within groups.

Search shall match:
- profile name,
- description,
- IP-related values,
- any future searchable metadata supported by the schema.

## PR-013 — Profile import/export

The user shall be able to:
- export one profile as JSON,
- import one JSON profile.

Multi-profile archive export is deferred.

## PR-014 — Corrupt profile files

A malformed profile must not prevent other profiles or the application from
loading.

Problematic profiles shall be represented in a dedicated problem state/group or
otherwise clearly surfaced to the user.

The application shall preserve the problematic file unless the user explicitly
deletes it.

## PR-015 — Profile file monitoring

When a profile JSON file is added, removed or modified externally while IPMan is
running, the profile list shall refresh automatically.

Filesystem monitoring must debounce duplicate/transient events.

## PR-016 — Initial profile storage

If required profile storage does not exist, IPMan shall create it automatically.

The application shall also capture the current configuration as initial
recoverable local data without interrupting the user with a first-run question.

This initial capture must not silently create confusing user-visible profile
names; detailed storage semantics are defined in the persistence specification.

## PR-017 — Profile comparison

Selecting a profile shall allow current and prospective values to be compared.

Fields that differ should be visually emphasized before application.

## PR-018 — Settings

Settings shall be opened from a small gear icon in a dialog/small window.

Settings include at minimum:
- theme: Follow Windows / Light / Dark,
- system tray behavior,
- close-button behavior,
- notifications enabled,
- apply profile immediately on selection,
- visibility of adapter categories where applicable,
- last-window state persistence.

## PR-019 — System tray

IPMan shall support system-tray operation.

On the first close/minimize-to-tray decision, the application shall allow the
user to choose desired behavior and persist that choice.

Tray activation shall restore the previous window size and position.

## PR-020 — Window state

IPMan shall remember:
- window size,
- window position,
- last selected adapter when it still exists.

The window shall be resizable with an enforced sensible minimum size.

## PR-021 — Single instance

Only one IPMan process instance shall own the interactive application at a time.

Launching IPMan again shall bring the existing instance to the foreground.

## PR-022 — Status bar

The main window shall contain a status bar that can show:
- selected adapter connection status,
- administrator state,
- last refresh time,
- application version,
- operation state such as Ready/Applying/Error.

## PR-023 — Quick actions

The main window shall provide quick actions including:
- DHCP,
- copy network information,
- ping gateway,
- flush DNS,
- renew IP where applicable.

These actions must operate only on the selected adapter where adapter scope is
relevant.

## PR-024 — Copy network information

The user shall be able to copy a concise text representation of selected
adapter information to the clipboard.

## PR-025 — Notifications

Successful or failed important operations shall use a Windows 11-style
notification/toast presentation where technically appropriate.

The product must also provide in-app state so critical feedback is not available
only through a transient toast.

## PR-026 — Crash handling

Unexpected fatal errors shall be logged as critical technical failures.

On the next launch, IPMan shall be able to identify that the previous session
terminated unexpectedly and show a short informational message.

## PR-027 — Logging

Initial release logging is restricted to critical technical failures, including:
- malformed/unreadable JSON,
- settings/profile write failures,
- unexpected Windows networking API failures,
- unhandled exceptions.

Routine user actions shall not be stored as an activity history.

## PR-028 — Diagnostics

A diagnostic panel shall provide support-oriented information such as:
- Windows version,
- application version,
- administrator status,
- adapter count,
- profile storage status,
- profile count.

The user shall be able to copy diagnostic information.

## PR-029 — Localization

All user-facing strings shall be localizable.

Initial release UI: Turkish only.

No user-facing string should be structurally tied to Turkish-only logic.

## PR-030 — Offline operation

Core IPMan operation shall require no Internet access and shall transmit no
configuration/profile data externally.

## PR-031 — Distribution

IPMan shall be distributable both as:
- portable executable/package,
- installer-based Windows application.

Automatic update checking is not part of the initial release.

## PR-032 — Administrator requirement

IPMan shall run elevated because network configuration is the core product
function.

Windows UAC shall be used.

The implementation shall avoid custom credential collection.
