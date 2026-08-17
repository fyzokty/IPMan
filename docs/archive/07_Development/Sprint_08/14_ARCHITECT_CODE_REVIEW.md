---
title: Sprint 08 Architect Code Review
version: 1.0.0
status: Changes Requested
date: 2026-08-08
---

# Review result

The Sprint 08 harness is directionally strong and safe-by-default, but it is not
approved for a real isolated mutation run yet.

Five corrections are required.

Do not run the destructive integration scenario yet.
Do not wire production Apply.
Do not begin Sprint 09.
Do not commit before final architect review.

# Accepted implementation

The following are approved:

- opt-in parsing occurs before adapter discovery/mutation,
- exact adapter GUID is required,
- no name/index/virtual-adapter heuristic,
- exact identity is checked through independent observer + production readers,
- production apply/recovery/mutation services are reused,
- evidence is written before mutation,
- raw evidence is separated from a sanitized summary,
- rollback snapshot is correlated and checked,
- production DNS mutation now supports `LeaveUnchanged`,
- default no-opt-in execution does not mutate an adapter,
- IPv6 and richer DNS observations are conceptually part of the gate.

# Required correction 1 — Refusal must not count as a passed destructive test

## Problem

When destructive opt-ins are absent, the destructive `[Fact]` logs `REFUSED`
and simply returns.

The test framework therefore counts the destructive case as PASSED.

That is safe from a mutation perspective, but it is bad evidence semantics.
A green test total can be misread later as "the destructive integration test
passed" even though no integration mutation occurred.

## Required behavior

The destructive case must be clearly represented as NOT EXECUTED / SKIPPED when
the opt-in gate is not satisfied.

Use the test-framework mechanism supported by the repository/xUnit version, or a
structurally equivalent design where default `dotnet test IPMan.sln` cannot
report the destructive scenario as passed.

Do not weaken the existing explicit opt-in gates.

The normal safe harness/parser/observer tests should still execute normally.

## Acceptance

A normal run should make it obvious from test output/counts that the destructive
scenario was not executed.

# Required correction 2 — Scenario must constrain unrelated configuration

## Problem

The harness validates:
- scenario name,
- starting mode,
- gateway presence/absence,
- DNS count shape.

It does not ensure that the operator-supplied desired configuration changes only
the dimension intended by the selected scenario.

Examples:

- `SetGateway` could accidentally be supplied with a different IPv4 address,
  mask or DNS list.
- `ClearGateway` could also change DNS/IP.
- `ManualDnsOne` could accidentally change gateway or IPv4.
- `AutomaticDns` could accidentally change IP/gateway.

That makes the scenario unsafe and weakens the meaning of its evidence.

## Required behavior

After the exact recovery state is read, validate the requested configuration
against the starting state.

For focused scenarios:

### SetGateway

Require:
- same IPv4 address,
- same subnet mask,
- DNS semantics unchanged,
- only gateway changes from absent -> requested gateway.

### ClearGateway

Require:
- same IPv4 address,
- same subnet mask,
- DNS semantics unchanged,
- only gateway changes from one gateway -> none.

### ManualDnsOne / ManualDnsTwo

Require:
- same IPv4 address,
- same subnet mask,
- same gateway semantics,
- only DNS intent changes.

### AutomaticDns

Require:
- same IPv4 address,
- same subnet mask,
- same gateway semantics,
- only DNS changes from Manual -> Automatic.

### StaticToStatic

May intentionally change IPv4/mask and may include explicitly requested
gateway/DNS changes. Its evidence should state exactly which dimensions changed.

### DhcpToStatic

Is inherently a broader transition and may set the supplied static fields.

If unchanged automatic/manual DNS semantics cannot be expressed safely by the
existing `StaticIpv4Configuration` input alone, fail closed and report the
limitation instead of asking the operator to guess.

Add deterministic tests for these scenario-specific guards.

# Required correction 3 — IPv6 evidence must include routes/default routes

## Problem

The current independent observer captures:

- IPv6 enabled support,
- IPv6 unicast addresses,
- gateway addresses,
- IPv6 DNS servers.

It does not capture the interface's IPv6 route table/default-route state.

The approved Sprint 08 non-interference gate requires IPv6 routes relevant to
the exact interface, not only the `GatewayAddresses` projection.

## Required behavior

Add a read-only Windows route observer using a documented Windows API.

Preferred direction:
- IP Helper API such as `GetIpForwardTable2`,
- filter to AF_INET6 and the exact interface,
- capture enough stable route information to detect unintended mutation.

At minimum preserve/compare:
- destination prefix,
- prefix length,
- next hop,
- interface identity/index/LUID as appropriate,
- route metric where meaningful.

Normalize away volatile fields that do not represent configuration, but do not
make the comparison so weak that route loss is missed.

No PowerShell parsing for this requirement unless a documented API proves
unusable and the architect explicitly approves a diagnostic-only fallback.

Add deterministic comparison tests.

# Required correction 4 — Unknown richer DNS property types must fail closed

## Problem

The V3 DNS observer decodes `DNS_SERVER_PROPERTY` only when `Type == 1` (DoH).
For other property types it records only:
- server index,
- type,
- null DoH fields.

That means a richer DNS property can change internally while the observer still
sees the same server index/type and incorrectly reports no change.

Modern Windows headers/documentation include property types beyond DoH (for
example DNS-over-TLS on newer platforms).

## Required behavior

Do not claim full richer-DNS non-interference when the observer encounters a
property type whose payload it does not understand.

Choose one safe approach:

1. implement documented observation for every property type supported by the
   target OS/API version used by the harness; or
2. mark the observation as `Incomplete/UnsupportedPropertyType` and fail the
   non-interference gate closed.

Do not dereference an unknown union member as DoH.

Record unknown property type values in local raw evidence.

The sanitized summary may report only that unsupported richer DNS state was
present.

Add tests showing an unsupported/unknown property prevents a false PASS.

# Required correction 5 — Sanitized summary must not contain raw exception text

## Problem

Raw scenario exceptions are stored as:

`<ExceptionType>: <exception.Message>`

and the same `Failure` field is copied into `summary.sanitized.json`.

Exception messages from WMI, filesystem, Windows APIs or future code can contain:
- adapter identifiers,
- paths,
- machine-specific values,
- internal DNS/network details.

Therefore the "sanitized" report is not guaranteed sanitized.

## Required behavior

Raw `result.json` may retain the local technical exception message.

`summary.sanitized.json` must contain only safe failure metadata, for example:
- exception type/category,
- stable architect-facing failure code,
- generic text that contains no raw OS/user/environment values.

Do not copy raw exception messages into the sanitized summary.

Add a test using a deliberately sensitive exception message and prove the
sanitized file/output does not contain it.

# Production DNS change — approved with future UI gate

The new `DnsMutationMode` design is approved for the mutation engine:

- LeaveUnchanged,
- Set,
- ClearToAutomatic.

Skipping `SetDNSServerSearchOrder` when current DNS semantics already match is
correct and reduces unrelated DNS/DoH risk.

However, before the production Apply UI is wired, the request layer must preserve
user DNS intent.

A UI textbox populated with an automatically supplied effective DNS address must
not accidentally convert Automatic DNS into Manual DNS merely because the text
contains the current effective server.

This is a future Apply/UI contract gate, not a Sprint 08 harness defect.

# Real-run gate

After these fixes:

1. Debug build,
2. Release build,
3. full default tests,
4. confirm destructive scenario is reported NOT EXECUTED/SKIPPED by default.

If no isolated adapter is designated, stop at:

`HARNESS_READY_REAL_RUN_PENDING`

Do not perform a real mutation.

Only after architect approval of the corrected harness will an isolated real-run
scenario be authorized.
