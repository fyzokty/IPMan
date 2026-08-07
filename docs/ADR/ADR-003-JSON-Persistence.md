---
title: ADR-003 - Use JSON for Settings and Profiles
status: Accepted
date: 2026-08-07
---

# Decision

Use JSON rather than SQLite/XML for initial local persistence.

Profiles are stored as separate JSON files.

Settings are stored as JSON.

Runtime data belongs under `%LocalAppData%\IPMan\`.

# Consequences

- Profiles are easy to export/import.
- Corruption can be isolated to one profile.
- Schema/version metadata is required for future migrations.
- File writes must be atomic/safe.
