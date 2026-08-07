---
title: ADR-011 - Named Mutex and Local IPC for Single Instance
status: Accepted
date: 2026-08-07
---

# Decision

Use a named mutex to establish the single interactive instance.

Use a local named pipe or equivalent local IPC signal so a second launch can ask
the first process to restore/focus its main window before the second exits.

# Security

IPC accepts only the minimal activation message; it is not a general remote
control API.
