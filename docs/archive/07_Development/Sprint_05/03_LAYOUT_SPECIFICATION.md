---
title: Sprint 05 Main Window Layout
version: 1.0.0
status: Approved
---

# Main visual hierarchy

## Header

Top area contains:
- product name `IPMan`,
- settings gear location may be reserved but does not need to be functional in
  this sprint.

Do not build a large ribbon/menu.

## Adapter tab strip

Directly below header.

Tabs must remain usable with several adapters.

If the number of adapters exceeds available width, use a normal WPF overflow/
scroll strategy rather than shrinking text to unreadable sizes.

## Main content

Use a two-panel working layout:

### Left — Current configuration

Read-only information card/panel.

Shows the selected adapter's actual Windows snapshot.

### Right — Configuration draft

Editable draft fields:
- IPv4,
- Mask,
- Gateway,
- Primary DNS,
- Secondary DNS.

Each field includes a copy action.

Below fields:
- `Mevcut Değeri Getir`

No active mutation actions in Sprint 05.

## Profile region

Do not implement the profiles panel yet.

The production layout should leave future expansion feasible, but do not add an
empty fake profile system merely to fill space.

## Status bar

Fixed to the bottom of the main window.

Must not consume excessive vertical space.

# Responsive behavior

At the minimum window width:
- labels remain readable,
- fields do not overlap,
- horizontal clipping of critical controls is avoided.

At larger widths:
- panels expand naturally,
- content should not stretch into unusably wide text fields without reasonable
  max-width/layout constraints.

# DPI

Use WPF device-independent layout.

Do not hard-code pixel positioning or Canvas-based page layout.
