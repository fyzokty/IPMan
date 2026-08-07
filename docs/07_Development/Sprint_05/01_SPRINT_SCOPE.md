---
title: Sprint 05 Scope
version: 1.0.0
status: Approved
---

# In scope

## Main window

Create the production layout foundation for the single main window.

The window shall:
- be resizable,
- retain the existing minimum-size principle,
- use a clean Windows/Fluent-inspired visual hierarchy,
- remain functional at the minimum supported window size,
- avoid unnecessary animation.

This sprint does not require a third-party Fluent UI package.

## Adapter tabs

All currently discoverable non-loopback adapters appear as tabs.

Each tab shall communicate:
- adapter name,
- connection state with both visual indicator and text/icon semantics.

The selected tab determines all displayed current/edit data.

The tab collection shall update when adapters are added/removed.

Selection shall remain on the same adapter identity where possible.

If the selected adapter disappears, choose a valid remaining adapter
deterministically.

## Current information panel

Display for selected adapter:

- connection status,
- adapter name,
- adapter description,
- MAC address,
- IPv4,
- subnet mask,
- default gateway,
- primary DNS,
- secondary DNS,
- DHCP/static/unknown mode,
- link speed,
- additional IPv4 addresses when present.

This panel is read-only.

Values must represent the latest verified discovery snapshot.

## Edit-draft panel

Provide text fields for:

- IPv4,
- subnet mask,
- gateway,
- primary DNS,
- secondary DNS.

These fields represent a UI draft only.

Changing them must not change Windows.

For this sprint:
- initialize draft from selected adapter current values,
- preserve user edits while that same adapter remains selected,
- define deterministic behavior when switching tabs,
- `Mevcut Değeri Getir` replaces the selected adapter draft with current values.

## Copy actions

Each edit field shall have a copy action/icon.

Copying:
- copies only that field's current draft text,
- does not mutate network state,
- handles empty values gracefully.

## Status bar

Show at minimum:
- selected adapter connection state,
- administrator state,
- last successful adapter refresh time,
- application version,
- application operation/read state (`Hazır`, `Yükleniyor...`, `Hata` as
  appropriate).

## Loading/empty/error states

The main window must represent:
- initial loading,
- no discoverable adapters,
- refresh failure while retaining last valid state when available.

Do not replace the entire window with modal errors for ordinary refresh failure.
