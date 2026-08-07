---
title: Error Handling and Logging Architecture
version: 1.0.0
status: Approved
---

# Error categories

## Validation error
Expected user-correctable input problem.

- no critical log required,
- localized field feedback,
- no Windows mutation.

## Conflict warning
Best-effort safety warning.

- not an error,
- user may continue.

## Recoverable infrastructure error
Examples:
- one corrupt profile,
- transient profile watcher read,
- unavailable optional adapter metadata.

Application remains usable.

Log only when troubleshooting value justifies it.

## Critical operation error
Examples:
- Windows configuration API fails,
- rollback write fails,
- settings/profile write cannot complete,
- adapter disappears during mutation.

Show user-friendly result and retain technical diagnostics.

## Fatal unhandled error
Capture last-resort critical log/crash marker and perform controlled shutdown.

# Technical/user separation

User message:
- Turkish,
- concise,
- actionable.

Technical diagnostics:
- API source,
- return/error code,
- exception type/message,
- relevant adapter identifier,
- timestamp.

Do not show raw stack traces in normal user dialogs.

# Logging scope

No routine audit/history log in release 1.0.

Never log credentials.

Avoid logging full machine/network inventory unless needed for a specific
critical diagnostic event.
