---
title: Sprint 06 Architect Code Review
version: 1.0.0
status: Changes Requested
date: 2026-08-07
---

# Review result

Sprint 06 architecture and implementation are broadly sound, but two corrections
are required before approval.

Do not begin Sprint 07 until these are fixed and validated.

# Accepted

The following implementation choices are approved:

- structured validation results,
- canonical IPv4 normalization,
- contiguous subnet-mask bit validation,
- explicit /31 and /32 host-address handling,
- field-level configuration differences,
- fresh adapter read before preflight,
- adapter identity based lookup,
- NoChange before conflict probing,
- explicit multiple-IPv4 safety condition,
- no probing of the selected adapter's own unchanged address,
- bounded one-shot ICMP conflict evidence,
- NoResponse remaining inconclusive evidence,
- no network mutation in Sprint 06.

# Required correction 1 — Reject self gateway

The current gateway validation checks:
- address class restrictions,
- same subnet,
- conventional network/broadcast endpoints.

It does not reject a gateway that is exactly equal to the adapter's requested
IPv4 address.

Examples that must be invalid:

- IP `192.168.1.50/24`, gateway `192.168.1.50`
- IP `192.168.1.0/31`, gateway `192.168.1.0`
- IP `192.168.1.77/32`, gateway `192.168.1.77`

For the release-1.0 model, an adapter cannot use its own requested address as its
default gateway.

Implementation requirements:

1. Add an explicit validation failure for this case.
2. Prefer a specific localizable validation code instead of hiding it behind a
   generic parse failure.
3. Add tests for ordinary subnet, /31 and /32 behavior.
4. Confirm that the peer endpoint of a /31 remains valid as a gateway when it is
   otherwise allowed.
5. For /32, an external gateway remains outside the configured subnet under the
   current release-1.0 rule; the host address itself must not become a loophole.

# Required correction 2 — Do not swallow arbitrary programming exceptions

`NetworkConfigurationPreflightService` currently catches almost every
`Exception`, excluding only a few catastrophic exception types, and converts it
to AdapterReadFailed or ProbeUnavailable.

This can hide defects such as:
- NullReferenceException,
- ArgumentException caused by a programming bug,
- invalid service state,
- unexpected implementation errors.

That conflicts with IPMan's diagnostic/error-handling architecture because an
unexpected defect can be converted into an ordinary preflight status with its
technical cause lost.

Required behavior:

## Adapter reader

Expected operational read failures may become a typed AdapterReadFailed result,
but do not use a catch-all "all exceptions except catastrophic" filter.

Use one of these architecture-safe approaches:

- catch a narrow documented set of recoverable exceptions, or
- introduce/use a dedicated application-level adapter-read exception that the
  Infrastructure implementation throws for expected OS/read failures.

Unexpected programming exceptions must remain visible to the application's
critical error handling instead of being silently swallowed.

## Conflict probe

The concrete `PingIpv4ConflictProbe` already converts its expected Ping/runtime
failures into `Ipv4ConflictProbeStatus.Unavailable`.

Therefore the preflight orchestrator should not need a blanket catch for every
unexpected exception.

Cancellation remains explicit.

Unexpected probe implementation defects should not be silently converted to
Unavailable.

## Tests

Add tests proving:
- expected operational failure maps to the intended typed result,
- unexpected programming exceptions are not silently swallowed,
- cancellation semantics remain intact.

# Sprint 07 architecture gates — no Sprint 06 implementation required

The following are not Sprint 06 defects, but they must be resolved before actual
Windows mutation is implemented.

## Full gateway state

The current UI/product model edits one gateway, but Windows can contain multiple
IPv4 default gateways.

Before mutation, architecture must define whether extra gateways are:
- preserved,
- explicitly replaced,
- or cause a safety condition.

Do not silently destroy unmodelled gateways.

## Full DNS state

The UI edits primary and secondary DNS, but Windows can contain more IPv4 DNS
servers.

Before mutation, architecture must define how additional DNS servers are handled.

Do not let a two-field UI silently erase unmodelled DNS entries.

## Probe semantics

`NoResponse` means:
- no positive conflict evidence was observed.

It does NOT mean:
- the address is guaranteed free.

`ProbeIndeterminate` is not proof of conflict either. The mutation workflow must
treat conflict probing as a warning/safety signal, not an authoritative address
ownership oracle.

# Validation after fixes

Run:

- `dotnet build IPMan.sln`
- `dotnet build IPMan.sln -c Release`
- `dotnet test IPMan.sln`

Expected:
- 0 warnings
- 0 errors
- all tests pass

Leave changes uncommitted for final architect review.

Provide:
- `git status --short`
- `git diff --stat`

Do not start Sprint 07.
