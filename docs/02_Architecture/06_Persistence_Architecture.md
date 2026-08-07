---
title: Persistence Architecture
version: 1.0.0
status: Approved
---

# Runtime root

Use:

`%LocalAppData%\IPMan\`

Logical structure:

- `Config\settings.json`
- `Profiles\*.json`
- `Backup\`
- `Logs\`
- `Temp\`

# Settings

One JSON settings document.

Must include schema version.

Writes must be safe:
1. serialize to temporary file in same logical storage area,
2. flush/close,
3. replace destination atomically where supported,
4. retain recoverable old/bad data when corruption handling requires it.

# Profiles

One JSON file per profile.

Advantages:
- single-profile import/export,
- corruption isolation,
- easy external management.

Filename is not trusted as the authoritative profile name.

JSON content contains profile identity/name.

# Profile watcher

Monitor `Profiles` directory.

Reload changed profile after debounce.

Malformed file:
- preserve it,
- report problem state,
- do not block healthy profiles.

# Rollback

Rollback data is separate from user profiles.

A rollback snapshot must record:
- adapter identity/context,
- captured timestamp,
- mode,
- relevant IPv4 values,
- gateway/DNS values.

Rollback storage is operational recovery data, not a normal user profile.

# Logs

Critical technical logs only.

No routine action-history database.
