---
title: ADR-015 - Store Profiles and Settings in the User Documents Folder
status: Accepted
date: 2026-08-20
---

# Decision

Split per-user storage by what the data is for.

User data — profiles and application settings:

`%UserProfile%\Documents\IPMan\`
- `Profiles\<name>.json`
- `settings.json`

Operational data — recovery snapshots and logs:

`%LocalAppData%\IPMan\`
- `Backup\recovery-*.json`
- `Logs\`

This supersedes [ADR-012](ADR-012-LocalAppData-Storage.md) for profiles and
settings. ADR-012 still governs operational data.

# Rationale

Profiles are documents in the ordinary sense: the user names them, copies them
between machines, mails them to a colleague and edits them in Notepad. Hiding them
under `%LocalAppData%` makes every one of those tasks harder for no benefit —
`Documents` is writable without elevation and is what the user already backs up.

Recovery snapshots are the opposite. They are written automatically before every
mutation, they are never meant to be opened by hand, and a user who deletes them
while tidying up loses a safety net without knowing it. They stay where ADR-012
put them.

[ADR-003](ADR-003-JSON-Persistence.md) is unchanged: JSON, one file per profile,
schema version required, writes atomic.

# Consequences

- The file name is presentation only. Identity is the `profileId` inside the JSON,
  as `docs/02_Architecture/06_Persistence_Architecture.md` requires. A rename
  writes the new file and then deletes the old one; a crash between the two leaves
  a duplicate, which the loader resolves by `profileId` keeping the newer
  `modifiedAtUtc`.
- Profile and settings JSON serialize enums as strings (`"mode": "Static"`), unlike
  `RecoverySnapshotJsonCodec`, which writes integers. These files are meant to be
  read and edited by hand; the snapshot format is not.
- IPMan runs `requireAdministrator` ([ADR-013](ADR-013-Single-Process-Elevation.md)).
  `Environment.GetFolderPath(SpecialFolder.MyDocuments)` therefore resolves to the
  Documents folder of the account that approved the UAC prompt. For the normal case —
  an administrator elevating their own session — this is the user's own folder. Under
  "run as different user" it is that account's folder. Accepted: it is the same
  account whose credentials started the process.
- If `MyDocuments` resolves to an empty path, fall back to `%LocalAppData%\IPMan\`
  rather than writing to the process working directory.
- Redirected Documents folders (OneDrive, roaming profiles) work unchanged, because
  the resolution goes through the Windows known-folder API.
