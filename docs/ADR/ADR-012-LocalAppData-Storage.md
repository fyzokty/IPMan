---
title: ADR-012 - Store Runtime Data in LocalAppData
status: Superseded by ADR-015
date: 2026-08-07
---

# Decision

Store per-user runtime data under:

`%LocalAppData%\IPMan\`

Portable distribution refers to executable deployment, not to storing mutable
profiles/settings beside the executable.

This avoids write-permission problems and gives both portable and installed
builds the same persistence semantics.

# Superseded

[ADR-015](ADR-015-User-Documents-Storage.md) moves profiles and settings to
`%UserProfile%\Documents\IPMan\`. This ADR still governs operational data:
recovery snapshots and logs remain under `%LocalAppData%\IPMan\`.
