---
title: Sprint 08 Acceptance Criteria
version: 1.0.0
status: Approved
---

# Harness acceptance

## AC-S08-001

Normal Debug/Release build remains 0 warnings / 0 errors.

## AC-S08-002

Normal `dotnet test IPMan.sln` remains non-destructive and passes.

## AC-S08-003

Destructive integration tests refuse to run without explicit enable flag,
isolated acknowledgement and exact adapter ID.

## AC-S08-004

No adapter is selected by name, index order or heuristic.

## AC-S08-005

Integration harness reuses production mutation/apply services.

## AC-S08-006

Before/after evidence is captured for the exact adapter.

## AC-S08-007

Rollback snapshot is captured and correlated with the run.

## AC-S08-008

Machine-specific integration artifacts are not accidentally committed.

# Real isolated-run acceptance

The following are required before the production Apply UI gate can be cleared.

## AC-S08-009

Static -> static real mutation is observed and verified.

## AC-S08-010

DHCP -> static real mutation is observed and verified.

## AC-S08-011

Gateway set is observed and verified.

## AC-S08-012

Gateway clear is observed and verified.

## AC-S08-013

Manual one/two-server DNS behavior is observed and verified.

## AC-S08-014

Empty/automatic DNS behavior is observed and verified.

## AC-S08-015

Any actual EnableStatic code 81 observation is handled correctly.

Absence of code 81 on the test machine is not itself a failure.

## AC-S08-016

Rollback snapshot accurately represents immediate pre-mutation recovery state.

## AC-S08-017

No unintended IPv6 configuration loss is observed.

## AC-S08-018

No unintended richer DNS/DoH-property loss is observed for the scenarios that
do not request DNS changes.

## AC-S08-019

If DoH-specific state could not be prepared, the report says `not executed`
rather than `passed`.

## AC-S08-020

No real mutation is performed on the normal development/production adapter.
