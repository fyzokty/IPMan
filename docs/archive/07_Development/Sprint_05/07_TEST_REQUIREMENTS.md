---
title: Sprint 05 Test Requirements
version: 1.0.0
status: Approved
---

# Required deterministic tests

## Selection

Test:
- first population selects deterministically,
- refresh preserves selected identity,
- removal selects fallback adapter,
- no adapters clears selection.

## Draft behavior

Test:
- draft initializes from current snapshot,
- editing marks draft dirty,
- switching adapters retains independent drafts,
- current refresh does not overwrite dirty draft,
- current refresh may update clean draft,
- Get Current Values resets values and dirty state.

## Formatting

Test:
- unavailable values,
- link-speed formatting,
- DHCP/static/unknown presentation,
- additional IPv4 presentation.

## Clipboard

If clipboard is abstracted:
- copy command receives exact draft field text,
- empty values do not crash.

Do not write brittle pixel-level UI tests.

Do not add destructive network tests.

# Existing Sprint 04 tests

All Sprint 04 tests must continue to pass.
