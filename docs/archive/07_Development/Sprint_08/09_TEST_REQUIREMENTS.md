---
title: Sprint 08 Test Requirements
version: 1.0.0
status: Approved
---

# Unit/default regression tests

Add tests for harness safety logic:

- disabled flag -> refuse/skip,
- missing adapter ID -> refuse/skip,
- invalid GUID -> refuse,
- missing isolated acknowledgement -> refuse,
- exact identity preserved,
- evidence directory generation,
- no target enumeration fallback.

These tests must be deterministic.

# Integration project tests

Mark destructive tests clearly.

They must not run during the default suite unless all deliberate opt-in
conditions are met.

# Production-code regression

All Sprint 04-07 tests must still pass.

# WMI behavior

Do not mock the real-run evidence and call that an integration pass.

Mocks/fakes prove harness orchestration only.

Real WMI behavior can only be reported as passed after an isolated Windows run.

# IPv6 / DNS observation tests

Pure parsing/normalization of observer results can be unit tested.

Actual non-interference is an isolated integration observation.
