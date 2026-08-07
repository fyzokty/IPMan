---
title: Security and Privilege Architecture
version: 1.0.0
status: Approved
---

# Elevation

IPMan uses an application manifest with:

`requireAdministrator`

Windows UAC provides elevation.

The application never asks the user to type administrator credentials into an
IPMan-controlled form.

# External input

Treat imported profile JSON and externally modified profile files as untrusted.

Validate:
- schema,
- supported schema version,
- lengths,
- IPv4 values,
- optional fields,
- filenames/path handling.

Never permit `..\` or arbitrary destination paths derived from profile names.

# Local storage

Use `%LocalAppData%\IPMan`.

Do not write runtime profile/config files beside the executable, because portable
distribution location may be read-only and per-user settings should remain
separate.

# Network behavior

Core operation is offline.

Do not add telemetry, cloud calls or update checks in release 1.0.

# Shell execution

No normal shell invocation for network configuration.

If future fallback is approved, arguments must be strongly constructed and never
formed through concatenating untrusted profile text into a shell command.
