---
title: IPMan Functional Requirements
version: 1.0.0
status: Approved
---

# Functional Requirements

## Adapter management

- **FR-ADP-001** Discover relevant Windows network adapters.
- **FR-ADP-002** Represent adapters as tabs.
- **FR-ADP-003** Show adapter connection indicator and display name.
- **FR-ADP-004** Allow editing disconnected adapters.
- **FR-ADP-005** Detect adapter additions automatically.
- **FR-ADP-006** Detect adapter removals automatically.
- **FR-ADP-007** Preserve last selected adapter when still available.
- **FR-ADP-008** Scope all adapter-specific commands to selected adapter.

## Current-state reading

- **FR-STA-001** Read current IPv4 configuration from Windows at startup.
- **FR-STA-002** Read DHCP/static mode.
- **FR-STA-003** Read mask, gateway and DNS.
- **FR-STA-004** Read MAC, description and link speed where available.
- **FR-STA-005** Refresh state after Windows/network change events.
- **FR-STA-006** Never treat requested values as verified current values.

## Editing and apply

- **FR-CFG-001** Provide editable IPv4/mask/gateway/DNS fields.
- **FR-CFG-002** Provide copy action for each edit field.
- **FR-CFG-003** Provide Get Current Values.
- **FR-CFG-004** Validate while editing.
- **FR-CFG-005** Prevent invalid apply.
- **FR-CFG-006** Treat gateway as optional.
- **FR-CFG-007** Treat DNS as optional.
- **FR-CFG-008** Detect no-op equivalent configuration.
- **FR-CFG-009** Capture rollback before mutation.
- **FR-CFG-010** Perform best-effort IP conflict warning.
- **FR-CFG-011** Allow override of conflict warning.
- **FR-CFG-012** Apply asynchronously.
- **FR-CFG-013** Verify by rereading Windows.
- **FR-CFG-014** Surface operation status and outcome.

## DHCP

- **FR-DHCP-001** Enable automatic IP addressing.
- **FR-DHCP-002** Enable automatic DNS as part of DHCP action.
- **FR-DHCP-003** Verify DHCP state after change.

## Profiles

- **FR-PRO-001** Store each profile as its own JSON file.
- **FR-PRO-002** Support static and DHCP profiles.
- **FR-PRO-003** Support optional description.
- **FR-PRO-004** Support favorite flag.
- **FR-PRO-005** Avoid mandatory physical-adapter binding.
- **FR-PRO-006** Default profile selection to load-only.
- **FR-PRO-007** Support configurable auto-apply.
- **FR-PRO-008** Generate safe numbered duplicate names.
- **FR-PRO-009** Search profiles.
- **FR-PRO-010** Show favorites first, then alphabetical.
- **FR-PRO-011** Import one JSON profile.
- **FR-PRO-012** Export one JSON profile.
- **FR-PRO-013** Confirm destructive profile deletion.
- **FR-PRO-014** Isolate malformed profiles.
- **FR-PRO-015** Watch profile directory for external changes.
- **FR-PRO-016** Compare profile/current values visually.

## Window and tray

- **FR-WIN-001** Use one primary main window.
- **FR-WIN-002** Main window is resizable.
- **FR-WIN-003** Persist size and position.
- **FR-WIN-004** Support notification-area operation.
- **FR-WIN-005** Persist close/tray behavior.
- **FR-WIN-006** Restore previous size/position from tray.
- **FR-WIN-007** Enforce single interactive instance.

## Settings/theme/localization

- **FR-SET-001** Open settings from gear action.
- **FR-SET-002** Theme choices: System, Light, Dark.
- **FR-SET-003** Persist profile auto-apply preference.
- **FR-SET-004** Persist notification/tray preferences.
- **FR-L10N-001** Initial UI language Turkish.
- **FR-L10N-002** All user strings are localizable resources.

## Support features

- **FR-SUP-001** Copy selected adapter network information.
- **FR-SUP-002** Ping gateway.
- **FR-SUP-003** Flush DNS.
- **FR-SUP-004** Renew IP where applicable.
- **FR-SUP-005** Show diagnostics panel.
- **FR-SUP-006** Copy diagnostics.
- **FR-SUP-007** Log critical failures.
- **FR-SUP-008** Detect prior unexpected termination.
