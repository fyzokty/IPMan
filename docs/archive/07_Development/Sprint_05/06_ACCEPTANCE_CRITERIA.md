---
title: Sprint 05 Acceptance Criteria
version: 1.0.0
status: Approved
---

# Acceptance Criteria

## AC-S05-001 — Clean build

Debug and Release builds complete with zero warnings and zero errors.

## AC-S05-002 — Production tab presentation

All discoverable non-loopback adapters appear as tabs using adapter identity
rather than tab index for state management.

## AC-S05-003 — Selection preservation

A background refresh does not change the selected adapter when that adapter
still exists.

## AC-S05-004 — Removal handling

If the selected adapter disappears, the UI selects a valid remaining adapter or
shows the no-adapter state.

## AC-S05-005 — Current information

The selected adapter current panel displays the latest discovered values and
distinguishes unavailable values.

## AC-S05-006 — Per-adapter draft

Edits made to adapter A remain separate from adapter B's draft while both remain
present in the running session.

## AC-S05-007 — Refresh does not destroy dirty draft

If adapter A has a dirty draft and Windows current state refreshes, the dirty
draft remains intact.

## AC-S05-008 — Get current values

`Mevcut Değeri Getir` resets the selected adapter's draft to its latest current
snapshot and clears dirty state.

## AC-S05-009 — Copy field

Each editable field can be copied independently without affecting other state.

## AC-S05-010 — No network mutation

No Sprint 05 command writes IP, mask, gateway, DNS or DHCP state.

## AC-S05-011 — Status bar

Status bar displays administrator state, selected adapter state, last successful
refresh time, version and current read/application status.

## AC-S05-012 — Localization

All new user-facing text is sourced from localization resources.

## AC-S05-013 — Loading

Initial UI can show a loading state while discovery completes without freezing.

## AC-S05-014 — Empty state

No-adapter environment results in a useful empty state and no exception.

## AC-S05-015 — Refresh failure

A refresh failure is represented in UI without discarding the last valid adapter
snapshot unless no valid snapshot exists.

## AC-S05-016 — Tests

Presentation logic for draft separation, dirty-state behavior, selection
preservation and formatting is covered by deterministic tests where practical.
