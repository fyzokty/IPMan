---
name: IPMan Fluent
colors:
  background: '#F3F3F3'
  surface: '#FFFFFF'
  outline: '#D1D1D1'
  on-surface: '#1B1B1B'
  on-surface-variant: '#5D5D5D'
  primary: '#0F6CBD'
  on-primary: '#FFFFFF'
  focus: '#005FB8'
  hover: '#E8F1FB'
  disabled: '#E5E5E5'
  success: '#107C10'
  success-container: '#DFF6DD'
  warning: '#B8860B'
  warning-container: '#FFF4CE'
  error: '#A80000'
  error-container: '#FDE7E9'
  info: '#0F6CBD'
  info-container: '#EFF6FC'
  connected: '#107C10'
  disconnected: '#6B6968'
dark-colors:
  background: '#202020'
  surface: '#2B2B2B'
  outline: '#3D3D3D'
  on-surface: '#FFFFFF'
  on-surface-variant: '#C5C5C5'
  primary: '#4CC2FF'
  on-primary: '#000000'
typography:
  title: { fontFamily: Inter, fontSize: 1.25rem, fontWeight: '600' }
  subtitle: { fontFamily: Inter, fontSize: 1rem, fontWeight: '600' }
  body: { fontFamily: Inter, fontSize: 0.875rem, fontWeight: '400' }
  caption: { fontFamily: Inter, fontSize: 0.75rem, fontWeight: '400' }
  mono: { fontFamily: JetBrains Mono, fontSize: 0.875rem, fontWeight: '400' }
rounded:
  control: 4px
  card: 8px
spacing:
  xs: 4px
  sm: 8px
  md: 12px
  lg: 20px
---

## Brand & Style

IPMan is a native Windows 11 desktop utility (WPF) that manages IPv4 settings and
saved profiles of network adapters. The look follows Windows 11 Fluent: calm neutral
grey window background, white cards with 1px hairline borders, a single blue accent,
no gradients, no hero imagery, no marketing chrome. Every screen is an application
window, never a web page: there is no browser chrome, no website navigation bar,
no footer and no cookie banners. All user-facing text is Turkish.

The audience is technicians and power users who switch between office, lab and PLC
networks several times a day. Density is moderate: information-rich but readable.

## Colors

- Window background `#F3F3F3`, cards `#FFFFFF`, hairline `#D1D1D1`.
- Primary text `#1B1B1B`, secondary text `#5D5D5D`.
- Accent `#0F6CBD` only for the primary action, the selected tab/list item and focus.
- Status: connected green `#107C10`, disconnected grey `#6B6968`.
- Inline result banners use container + border pairs: success, warning, error, info.
- Dark theme mirrors the same roles (background `#202020`, card `#2B2B2B`, accent `#4CC2FF`).

## Typography

Inter stands in for Segoe UI Variable. Titles 20px semibold, section headers 16px
semibold, body 14px, captions 12px. IP addresses, masks, gateways and DNS values use a
monospace face so digits align.

## Layout & Spacing

Main window minimum 1100×700. Header strip on top, status bar at the bottom, a fixed
280px profiles pane on the left, adapter tabs filling the rest. Cards use 20px padding
and 12px gaps. Dialogs are small, centered on the owner window, with right-aligned
buttons (primary first from the right).

## Shapes

Controls 4px radius, cards and dialogs 8px radius. Pills only for status dots and badges.

## Components

- Buttons: primary filled accent; secondary white with hairline border; subtle (text only).
- Text boxes: white, 1px border, 2px accent underline on focus; invalid fields show a
  red border and a red caption below.
- Tabs: Windows 11 style with an accent underline on the selected adapter tab; each
  tab shows a status dot (connected/disconnected) before the adapter name.
- Lists: profile rows 36px high, star icon for favorites, selected row light-accent fill
  with a 3px accent bar on the left.
- Inline result banner: icon + one-line Turkish message + close (×), colored by severity.
- Status bar: 28px high, secondary text, items separated by thin vertical dividers.
- Icons: Fluent System Icons style, 16px line icons.
