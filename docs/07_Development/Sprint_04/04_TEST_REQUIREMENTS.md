---
title: Sprint 04 Test Requirements
version: 1.0.0
status: Approved
---

# Test strategy

Claude should introduce the minimum appropriate test project(s) required by the
current architecture.

Do not build a large UI automation suite in this sprint.

# Required test categories

## Mapping tests

If adapter API objects are mapped through a separate mapper, test:

- connected state,
- IPv4 selection,
- subnet mask,
- gateway,
- DNS ordering,
- missing optional fields,
- MAC formatting,
- configuration-mode mapping where supported.

Prefer mapper logic that can be tested without modifying the local machine.

## Monitor lifecycle tests

Where feasible through a wrapper/abstraction:

- start subscribes once,
- repeated start does not duplicate subscription,
- stop unsubscribes,
- dispose unsubscribes,
- repeated stop is safe,
- event is translated to application `Changed` event.

If direct static `NetworkChange` events make deterministic testing difficult,
introduce only the smallest internal abstraction necessary.

Do not distort public architecture merely to satisfy mocking.

## Failure tests

Test graceful behavior for:
- no adapters,
- missing IPv4,
- missing gateway,
- missing DNS,
- unsupported/unknown configuration mode.

# Test naming

Use behavior-focused names.

Examples of style only:

`GetAdapters_WhenGatewayIsMissing_ReturnsSnapshotWithoutGateway`

Do not blindly copy example names if implementation differs.

# Test safety

Automated Sprint 04 tests must be read-only.

They must never change the developer machine's network configuration.
