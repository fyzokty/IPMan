---
title: Production Apply Release Gate
version: 1.0.0
status: Approved
---

# Production Apply remains closed

Sprint 08 implementation alone does NOT authorize wiring `Uygula`.

The architect will only open the production Apply gate after reviewing real
isolated-run evidence.

# Gate states

## NOT_READY

Any of:
- harness not implemented,
- default tests can mutate adapters,
- wrong-adapter risk exists,
- build/tests failing.

## HARNESS_READY_REAL_RUN_PENDING

Harness is safe and tested, but no approved isolated real run has completed.

This is an acceptable Sprint 08 development result.

Production Apply remains disabled/unwired.

## INTEGRATION_VALIDATED

Required real scenarios have passed with evidence and no architecture blocker
remains.

Only after architect approval may a later sprint wire production Apply.

# Blocker examples

Keep gate closed if:

- WMI provider behavior differs from documented assumptions,
- gateway clear is unreliable,
- automatic DNS restore semantics are ambiguous,
- DNS/DoH metadata is unintentionally destroyed,
- IPv6 state is changed,
- rollback snapshot does not match immediate pre-state,
- product reports success without actual equivalence.
