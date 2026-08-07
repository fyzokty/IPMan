---
title: Sprint 06 Acceptance Criteria
version: 1.0.0
status: Approved
---

# Acceptance Criteria

## AC-S06-001

Debug and Release builds complete with zero warnings and zero errors.

## AC-S06-002

Valid ordinary static IPv4 configurations produce a normalized valid result.

## AC-S06-003

Malformed IPv4, IPv6-in-IPv4-field and invalid subnet masks are rejected without
throwing an application-level exception.

## AC-S06-004

Subnet masks with non-contiguous bits are rejected.

## AC-S06-005

Gateway is optional.

An empty gateway validates successfully.

## AC-S06-006

A supplied gateway outside the configured subnet is rejected for the release-1.0
configuration model.

## AC-S06-007

Both DNS fields may be empty.

Valid IPv4 DNS addresses are accepted.

## AC-S06-008

Secondary DNS without primary DNS is rejected rather than silently reordered.

## AC-S06-009

Equivalent current/desired configurations return NoChange after normalization.

## AC-S06-010

Comparison identifies field-level differences.

## AC-S06-011

Preflight uses adapter identity and returns AdapterUnavailable if that exact
adapter disappears.

## AC-S06-012

An adapter with additional IPv4 assignments returns an explicit safety condition
rather than losing those assignments.

## AC-S06-013

A positive conflict probe is represented as a potential conflict warning.

## AC-S06-014

A non-responsive address is represented as inconclusive/not-observed and never
as guaranteed free.

## AC-S06-015

Conflict-probe errors do not crash preflight.

## AC-S06-016

No Sprint 06 code changes IP, mask, gateway, DNS or DHCP state.

## AC-S06-017

All existing Sprint 04/05 tests remain passing.

## AC-S06-018

All new deterministic validation/comparison/preflight behavior is unit tested.
