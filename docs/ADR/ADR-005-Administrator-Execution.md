---
title: ADR-005 - Run IPMan Elevated
status: Accepted
date: 2026-08-07
---

# Decision

The application shall require administrator elevation for normal execution.

Use Windows UAC through the application manifest/execution model.

Do not collect or store administrator credentials.
