---
title: IPMan Edge Cases
version: 1.0.0
status: Approved
---

# Edge Cases

## EC-001 — No adapters found

Show an empty/diagnostic state rather than crashing.

Keep settings/profile management available where reasonable.

## EC-002 — Selected adapter disappears during apply

Treat operation as failed/indeterminate.

Do not report success.

Preserve rollback data and critical diagnostic details.

## EC-003 — Adapter changes between validation and apply

Re-read identity/state immediately before mutation where technically practical.

Never silently redirect the requested change to a different adapter.

## EC-004 — Link disconnected

Allow editing and applying configuration if Windows permits it.

Connection status remains `Bağlı Değil`.

## EC-005 — Multiple IPv4 addresses

The detailed architecture must define how the editable "primary" IPv4 is
selected without silently destroying unrelated addresses.

Until that architecture is approved, implementation must not assume only one
IPv4 can exist.

## EC-006 — Multiple gateways/DNS servers

Display and edit primary/secondary values according to the approved data model.

Do not lose additional Windows values without an explicit architecture decision.

## EC-007 — Ping does not respond

A failed ping does not prove that an IP is unused.

Conflict detection language must say "may be in use" only when evidence suggests
use; it must never claim perfect collision detection.

## EC-008 — Same configuration requested

Show an informational result and perform no configuration mutation.

## EC-009 — Profile name collision

Generate the next available numbered name atomically enough to avoid accidental
overwrite.

## EC-010 — Profile JSON edited while being read

Debounce/retry transient filesystem changes before declaring corruption where
appropriate.

## EC-011 — Corrupt JSON

Keep file, mark/surface problem, load remaining profiles, log technical failure.

## EC-012 — Settings JSON corrupt

Recover with safe defaults and preserve/backup problematic file where possible.

Do not prevent the user from opening the application.

## EC-013 — LocalAppData not writable

Show a clear failure because profiles/settings/rollback reliability cannot be
guaranteed.

Log technical detail.

## EC-014 — Second IPMan launch

Signal first instance, restore/focus it, exit second instance.

## EC-015 — Window stored off-screen

If monitor topology changed, restore the window into a visible screen region
instead of blindly applying stale coordinates.

## EC-016 — Last selected adapter no longer exists

Choose a valid available adapter and update persisted state.

## EC-017 — Theme follows Windows

React to Windows theme change while running when implementation supports reliable
theme-change observation.

## EC-018 — User closes during network mutation

Prevent unsafe immediate termination or clearly coordinate cancellation/finish
so configuration state is not abandoned mid-transition.

## EC-019 — Unexpected crash after snapshot but before verification

On next start retain the snapshot and unexpected-shutdown information so support
or recovery remains possible.

## EC-020 — Profile imported with unsafe filename

The external filename must not control arbitrary filesystem paths.

Derive/sanitize the destination filename within the Profiles directory.

## EC-021 — Profile imported with future schema version

Do not silently reinterpret unknown fields/version.

Surface incompatibility while preserving the source file.

## EC-022 — DHCP adapter has no lease yet

After enabling DHCP, Windows may temporarily have no usable IPv4.

Represent the actual transitional state truthfully; do not invent an address.

## EC-023 — Gateway empty on static profile

Apply static address/mask without requiring a default route when Windows API
supports it.

## EC-024 — DNS empty on static profile

Do not invent public DNS values.

Use the semantics defined by architecture for empty DNS while respecting user
intent.
