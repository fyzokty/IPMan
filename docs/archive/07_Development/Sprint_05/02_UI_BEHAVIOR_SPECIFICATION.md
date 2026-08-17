---
title: Sprint 05 UI Behavior Specification
version: 1.0.0
status: Approved
---

# 1. Adapter selection model

Adapter selection is identity-based, never tab-index based.

When a refresh produces a new adapter collection:

1. If the previously selected adapter identity still exists, keep it selected.
2. Otherwise select the first adapter in the deterministic displayed order.
3. If no adapters exist, enter the no-adapter state.

# 2. Draft state per adapter

Each adapter needs an independent edit draft.

Example:

- User edits Ethernet draft.
- User switches to Wi-Fi.
- User switches back to Ethernet.
- The unsaved Ethernet draft should still be present while the adapter exists.

A network refresh must not silently overwrite a dirty draft.

The UI should separately represent:
- current Windows snapshot,
- current edit draft.

`Mevcut Değeri Getir` is the explicit action that resets the selected draft to
the latest current snapshot.

If an adapter is removed, its in-memory draft may be discarded.

No persistence of drafts across application restart is required.

# 3. Current-state refresh vs draft

When Windows current state changes:

- current information panel updates,
- clean drafts may be synchronized automatically,
- dirty drafts must not be silently replaced.

If Claude introduces a `IsDirty`/equivalent state, keep it presentation-focused
and testable.

# 4. Connection status

Do not rely on color alone.

The selected adapter details must use Turkish text:
- `Bağlı`
- `Bağlı Değil`

Tabs may use a compact icon/status marker, but accessibility semantics must still
be meaningful.

# 5. Missing values

Use a consistent presentation such as `—` for unavailable read-only values.

Do not write `0.0.0.0` or fabricated values merely to fill empty UI.

Draft fields corresponding to absent optional values may be empty.

# 6. Link speed

Render a human-readable value when available.

Examples of acceptable presentation:
- `100 Mbps`
- `1 Gbps`
- `2.5 Gbps`

Formatting logic must be deterministic and testable, not embedded as ad-hoc XAML
string manipulation.

# 7. Additional IPv4 addresses

The release-1.0 editor continues to target one primary IPv4 address.

Additional addresses are informational in Sprint 05.

They must not be dropped from the ViewModel projection.

# 8. No fake functionality

Do not wire placeholder Apply/DHCP buttons to fake success messages.

If future buttons are visible for layout purposes, they must be clearly disabled.
Prefer omitting them until the mutation sprint.
