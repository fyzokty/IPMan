---
title: IPv4 Validation and Normalization Specification
version: 1.0.0
status: Approved
---

# General rule

Use .NET networking types/logic.

Do not implement IPv4 correctness using regex alone.

# IPv4 address

Required for static configuration.

Accept only IPv4 (`AddressFamily.InterNetwork`).

Reject:
- malformed input,
- IPv6,
- unspecified `0.0.0.0`,
- multicast,
- broadcast `255.255.255.255`,
- loopback addresses for normal adapter configuration.

Do not reject private, link-local/APIPA or public ranges merely because of their
range.

# Subnet mask

Required.

Accept dotted-decimal IPv4 subnet masks.

The bit pattern must be contiguous ones followed by contiguous zeros.

Examples:

Valid:
- `255.255.255.0`
- `255.255.0.0`
- `255.255.255.252`

Invalid:
- `255.0.255.0`
- `255.255.128.255`

Do not use regex as the final contiguity check.

# Gateway

Optional.

If present:
- must parse as IPv4,
- must not be unspecified/multicast/broadcast/loopback,
- should be reachable within the configured IPv4 subnet for the normal v1
  configuration model.

If gateway is outside the configured subnet, return a validation failure rather
than silently accepting a configuration that is unlikely to work for the target
users.

# DNS

Primary and secondary DNS are independently optional.

If supplied:
- must parse as IPv4,
- must not be unspecified/multicast/broadcast.

Loopback DNS (`127.0.0.1`) is allowed because a local DNS resolver is a valid
advanced configuration.

If primary is empty and secondary is populated, do not silently reorder user
intent. Treat this as a validation error for the release-1.0 two-field model.

Both empty is valid.

# Address vs subnet

Reject the ordinary subnet network address and broadcast address when the subnet
has conventional host bits.

Do not implement simplistic assumptions that break /31 or /32; if supporting
those masks requires explicit special handling, cover it with tests and document
the behavior.

# Validation result

Do not throw for ordinary invalid user input.

Return structured validation information that can later support:
- per-field UI error messages,
- apply blocking,
- localization.

Technical exceptions remain for programming/OS failures, not user mistakes.
