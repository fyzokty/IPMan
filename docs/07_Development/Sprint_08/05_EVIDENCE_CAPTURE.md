---
title: Integration Evidence Capture
version: 1.0.0
status: Approved
---

# Purpose

A destructive networking test must leave enough evidence for architect review.

# Evidence directory

Keep integration evidence out of normal application runtime storage.

Use a repo-local ignored directory or test-results directory such as:

`artifacts/integration/sprint08/<timestamp>/`

Do not commit machine-specific network addresses or personal environment details
unless intentionally sanitized.

Add the evidence directory to `.gitignore` if needed.

# Per-run evidence

Generate machine-readable JSON where practical.

At minimum record:

- run ID,
- UTC timestamp,
- Windows version/build,
- app/test assembly version,
- exact adapter ID,
- adapter display metadata,
- requested scenario,
- before IPv4 state,
- before gateway+metric,
- before DNS source/list/richer settings,
- before IPv6 observation,
- rollback snapshot ID,
- mutation per-step return codes,
- apply result,
- after IPv4 state,
- after gateway+metric,
- after DNS state,
- after IPv6 observation,
- pass/fail,
- explicit differences.

# Sensitive data

Do not upload or commit:
- unrelated adapter details,
- public IP information not needed for the test,
- Wi-Fi credentials,
- DNS suffixes/internal names if not needed,
- machine/user identifiers beyond what is required for debugging.

Provide a sanitization helper or clear manual redaction guidance for the report
sent to the architect.

# Failure

On failure:
- keep evidence,
- do not auto-delete rollback snapshot,
- stop further destructive scenarios unless continuing is clearly safe.
