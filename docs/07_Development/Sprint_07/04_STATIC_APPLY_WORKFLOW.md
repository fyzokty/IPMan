---
title: Static Apply Application Workflow
version: 1.0.0
status: Approved
---

# Application service

Introduce/evolve an application-layer service that orchestrates static apply.

The UI will call this service in a future sprint.

# Request

The request must identify:
- exact adapter ID,
- desired static configuration,
- whether the caller explicitly chose to continue after a potential conflict.

Do not encode confirmation state in a global variable.

# Workflow

## 1. Preflight

Run the Sprint 06 preflight from a fresh adapter read.

## 2. Preflight status handling

- ValidationFailed -> stop.
- AdapterUnavailable -> stop.
- AdapterReadFailed -> stop.
- MultipleIpv4RequiresSafetyDecision -> stop.
- NoChange -> return no-change; no rollback file and no WMI mutation needed.
- PotentialAddressConflict + caller has not confirmed -> stop with warning result.
- PotentialAddressConflict + caller explicitly confirmed -> continue.
- ProbeIndeterminate -> may continue only as an indeterminate warning state
  according to the typed request/result design; it is not proof of conflict.
- Cancelled -> stop.

## 3. Full topology safety

Evaluate complete gateways/DNS collections from the fresh snapshot.

Block if:
- >1 IPv4 gateway,
- >2 IPv4 DNS servers.

## 4. Capture rollback

Persist rollback snapshot.

If persistence fails -> stop.

## 5. Mutate

Invoke `INetworkAdapterConfigurator.ApplyStaticAsync` or the evolved approved
contract.

## 6. Fresh verification

Never trust the mutation return code alone.

Perform bounded fresh-read reconciliation.

Use a deterministic/testable delay abstraction.

Do not wait indefinitely.

## 7. Compare actual to desired

Use the existing normalized comparer plus the required full-state checks.

## 8. Result

Return one explicit outcome such as:
- NoChange
- ConflictConfirmationRequired
- SafetyBlocked
- RollbackCaptureFailed
- MutationFailed
- PartialFailure
- VerificationFailed
- VerifiedSuccess
- Cancelled

Exact enum naming may vary.

# Success rule

Only `VerifiedSuccess` means IPMan may later display a success notification.

A WMI `0` return code alone is NOT success at the product level.
