---
title: Sprint 08 Architect Final Review
version: 1.1.0
status: Changes Requested
date: 2026-08-08
---

# Review result

The five corrections from the previous architect review are accepted.

The harness is close to approval, but three additional issues were found during
the final pre-destructive-run review.

Do not perform the real destructive run until these are corrected.

# Previously requested corrections — ACCEPTED

Approved:

1. No-opt-in destructive test is represented by xUnit Skip/NOT EXECUTED.
2. Focused scenarios reject unrelated requested IPv4/gateway/DNS changes.
3. Exact-interface IPv6 routes are observed through GetIpForwardTable2.
4. Unknown richer DNS property payloads make non-interference fail closed.
5. Sanitized evidence no longer contains raw exception text.

# Required correction 6 — DNS non-interference must follow requested dimensions

## Problem

`ObservationComparer.CompareNonInterference` currently decides whether DNS
name-server state may change from the broad scenario enum.

It currently treats scenarios such as:
- StaticToStatic
- DhcpToStatic

as DNS-changing scenarios even when DNS was NOT one of the requested dimensions.

Example:

Starting state:
- Static IPv4
- Automatic DNS

Scenario:
- StaticToStatic
- only IPv4 address changes
- DNS intent remains unchanged

If EnableStatic unexpectedly changes DNS source/name-server state, the current
observer can mask the NameServer flag/value difference because StaticToStatic is
classified broadly as "changes DNS".

That would create a false PASS.

## Required behavior

Non-interference must be based on the actual approved requested dimensions from
`ScenarioPreconditionResult`, not only the scenario name.

The comparison should receive or otherwise know:

`DnsDimension requested: true/false`

If DNS was not requested:
- compare DNS source/name-server state fully,
- compare DNS flags fully,
- richer DNS state remains protected.

If DNS was explicitly requested:
- allow only the DNS dimensions that the test intentionally changes,
- continue protecting unrelated profile/supplemental/richer DNS state.

Add tests:

1. StaticToStatic, only IPv4 changes, DNS NameServer changes -> FAIL.
2. StaticToStatic with explicit DNS change -> requested DNS change is allowed.
3. DHCPToStatic with DNS dimension not requested -> unrelated DNS state change
   cannot be hidden.
4. Focused gateway scenario remains strict.

# Required correction 7 — Preserve existing gateway metric when gateway address is unchanged

## Problem

The product editor models a gateway address but does not expose gateway metric.

The current mutation implementation sends:

`GatewayCostMetric = 1`

when a gateway is set.

This means a static adapter with:

- gateway `192.168.1.1`
- metric `25`

can have its metric silently changed to `1` when the user changes only:
- IPv4 address/mask, or
- DNS,

even though gateway was not requested as a changed dimension.

The integration harness currently also expects metric 1 after mutation, so it
can normalize this unintended state loss into an expected result.

This violates the rule that IPMan must not silently modify state the UI does not
represent.

## Required production behavior

Evolve the mutation plan so gateway metric semantics are explicit.

At minimum:

### Existing gateway address remains unchanged

If:
- there is exactly one existing gateway,
- desired gateway address equals the existing gateway,
- recovery state contains a reliable metric,

then any necessary SetGateways call must preserve that existing metric.

Do not replace it with 1 merely because another field changes.

### New/different gateway

For a user-requested new/different gateway, release-1.0 may use the approved
default metric 1.

### Gateway clear

Clear semantics remain as previously approved; metric is not preserved because
the gateway is being removed.

### Missing metric

If an unchanged gateway must be re-applied but its metric cannot be read
reliably, fail closed rather than inventing a value.

## Model

The exact implementation is up to Codex, but the mutation plan must contain
enough information for Infrastructure to know the intended gateway metric.

Do not make Infrastructure rediscover policy.

## Harness behavior

The integration verifier must distinguish:

- gateway dimension requested -> expected new gateway behavior/metric,
- gateway dimension not requested -> address AND metric must remain equal to the
  pre-mutation recovery state.

Remove the current workaround that simply refuses focused DNS scenarios when the
existing metric is not 1 once preservation is correctly supported.

Add deterministic tests for:

- unchanged gateway metric 25 survives IP-only static mutation,
- unchanged gateway metric 25 survives DNS-only mutation,
- new gateway uses metric 1,
- clear removes gateway,
- missing metric fails closed when preservation is required.

# Required correction 8 — DNS V3 downgrade must not hide an observation failure

## Problem

`DnsSettingsObserver` currently attempts:

V3 -> V2 -> V1

and falls back whenever a newer call returns any non-zero result.

Microsoft documents GetInterfaceDnsSettings as:

- NO_ERROR = success
- any non-zero value = failure.

A V3 call can therefore fail for a reason other than "this Windows version does
not support V3".

Blindly retrying V2 and then marking richer-property observation complete can
hide an actual V3 observation failure and create a false non-interference PASS.

## Required behavior

Downgrade only when platform capability is known to make that version
inapplicable.

Acceptable approaches include:

- use the documented Windows build/version capability boundary before selecting
  V3/V2/V1; or
- use another documented, deterministic capability check.

Do not treat an arbitrary V3 API failure as permission to silently downgrade.

If the selected highest supported version fails:
- record the native error,
- mark DNS observation incomplete,
- fail the non-interference gate closed.

## Older Windows

The application supports Windows 10/11.

For a platform that legitimately cannot expose V3 richer DNS properties:
- record the platform-selected observation version,
- do not claim that a failed V3 call succeeded indirectly,
- make clear whether richer DNS state is NotApplicableByPlatform or Complete.

Do not conflate:
- Not supported by this OS
with
- supported API failed to read state.

## Tests

Add deterministic version-selection tests and failure tests:

1. V3-capable platform + V3 native failure -> incomplete/fail closed, no silent
   V2 success.
2. Platform below V3 capability -> intentional older-version observation path.
3. V3 success -> richer property observation retained.
4. Unknown property type still fails closed.

# Microsoft documentation basis

Use Microsoft Learn as primary source.

Relevant documented facts:

- `GetInterfaceDnsSettings` returns NO_ERROR on success and non-zero on failure.
- The Version member selects DNS_INTERFACE_SETTINGS V1, EX/V2, or SETTINGS3/V3.
- DNS_INTERFACE_SETTINGS3 has its own minimum supported Windows build.
- DNS_SERVER_PROPERTY_TYPE may contain richer property types beyond DoH on
  newer SDK/Windows versions.

Do not infer API capability from a failed call alone when a platform/build check
can establish it deterministically.

# Validation

After fixes run:

- `dotnet build IPMan.sln`
- `dotnet build IPMan.sln -c Release`
- `dotnet test IPMan.sln`

Expected:
- zero warnings/errors,
- all normal tests pass,
- destructive test is skipped/not executed by default.

Do not run a real destructive scenario.

Return a concise completion report and leave changes uncommitted.
