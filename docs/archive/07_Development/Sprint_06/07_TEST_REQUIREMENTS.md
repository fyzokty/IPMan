---
title: Sprint 06 Test Requirements
version: 1.0.0
status: Approved
---

# Validation tests

Cover representative boundaries, including:

- normal private IPv4,
- public IPv4,
- APIPA,
- malformed address,
- IPv6 input,
- 0.0.0.0,
- loopback main address,
- multicast,
- broadcast,
- valid subnet masks,
- non-contiguous masks,
- gateway empty,
- gateway in subnet,
- gateway outside subnet,
- both DNS empty,
- one DNS,
- two DNS,
- secondary without primary,
- local DNS 127.0.0.1,
- network/broadcast host calculations,
- /31 and /32 explicitly.

# Normalization/comparison tests

Cover:
- whitespace normalization,
- equivalent values,
- gateway added/removed,
- DNS differences,
- DHCP/current-mode mismatch,
- field difference set,
- additional IPv4 awareness.

# Preflight tests

Using fakes:
- adapter found,
- adapter disappears,
- no-change bypasses unnecessary conflict warning,
- multiple IPv4 safety condition,
- positive conflict probe,
- no-response probe,
- probe error,
- cancellation.

# Conflict probe infrastructure

Do not make normal unit tests depend on internet or LAN response.

If the concrete Ping implementation receives an integration test, it must be
clearly separated and must not assume a specific external host is reachable.

# Test performance

No tests should wait real multi-second ping timeouts.

Use abstractions/fakes for orchestration tests.
