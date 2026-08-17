---
title: Sprint 07 Test Requirements
version: 1.0.0
status: Approved
---

# Full state tests

Test:
- zero/one/multiple IPv4 gateways,
- zero/one/two/three+ IPv4 DNS servers,
- ordered preservation,
- convenience primary/secondary projections.

# Safety tests

Test:
- multiple IPv4 block,
- multiple gateway block,
- >2 DNS block,
- exact adapter identity.

# Rollback tests

Test:
- correct snapshot contents,
- all IPv4/gateway/DNS values preserved,
- atomic repository behavior through filesystem abstraction/test directory,
- persistence failure aborts before configurator call.

# Orchestration tests

Test:
- validation failure,
- no-change,
- conflict unconfirmed,
- conflict confirmed,
- probe indeterminate semantics,
- configurator success + immediate verify,
- configurator success + delayed verify,
- configurator success + verification failure,
- partial mutation failure,
- adapter disappears during verification,
- cancellation before mutation,
- cancellation after mutation has begun,
- serialization of concurrent mutations.

# Configurator tests

Do not fake WMI return-code translation inside the class under test.

Extract only the smallest internal seam needed so return-code mapping and step
result logic can be tested without changing Windows.

# Regression

All existing tests must continue to pass.
