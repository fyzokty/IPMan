---
title: Error and Result Model
version: 1.0.0
status: Approved
---

# User-facing vs technical detail

Application result objects may retain:
- technical operation stage,
- WMI code,
- exception/diagnostic category,
- rollback snapshot ID/path reference.

User-facing Turkish messages remain an App localization concern.

# Do not swallow defects

Continue the Sprint 06 rule:

- expected operational failures -> typed results,
- unexpected programming defects -> critical error handling.

Do not catch every `Exception` and convert it into "mutation failed".

# Low-level configurator result

The configurator should make partial execution observable.

Example conceptual fields:
- IP step attempted/succeeded,
- gateway step attempted/succeeded,
- DNS step attempted/succeeded,
- restart required,
- technical failure code.

Exact representation is implementation choice.

# Apply result

Application-level apply result must state:
- final outcome,
- verified snapshot when available,
- rollback snapshot reference when captured,
- comparison/verification information,
- technical failure metadata where relevant.

Avoid boolean-only result types.
