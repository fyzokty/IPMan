# Sprint 06 Completion Report

## Build

- Debug: `dotnet build IPMan.sln` — succeeded
- Release: `dotnet build IPMan.sln -c Release` — succeeded
- Warnings: 0
- Errors: 0

## Tests

- Total: 229
- Passed: 229
- Failed: 0
- Skipped: 0

73 deterministic Sprint 06 test cases were added. All 156 existing Sprint
04/05 tests remain passing. No unit test sends an ICMP packet or mutates a
network adapter.

## Git review

Changes are uncommitted. The required `git diff --stat` output for tracked files
is:

```text
 src/IPMan.App/App.xaml.cs                               | 5 +++++
 src/IPMan.Domain/Networking/StaticIpv4Configuration.cs | 5 ++---
 2 files changed, 7 insertions(+), 3 deletions(-)
```

All newly added Sprint 06 source and test files are untracked and therefore do
not appear in `git diff --stat`; they are listed by `git status --short` and
summarized below.

## Files changed

- Domain: structured field/error validation results and typed configuration
  difference results; clarified that raw `StaticIpv4Configuration` input is
  normalized by validation.
- Application: IPv4 parsing/subnet arithmetic, validator, comparer, conflict
  probe abstraction and fresh-read preflight orchestration/result contracts.
- Infrastructure: one-attempt, 500 ms default ICMP probe with a hard five-second
  maximum and explicit response/no-response/unavailable/cancelled semantics.
- App: DI registrations only; no UI action or mutation wiring.
- Tests: validation boundaries, normalization, `/31` and `/32`, field
  comparison, identity/fresh-read preflight, multiple-address safety, probe
  outcomes/errors/cancellation and deterministic concrete-probe input/options.

## Behavior implemented

- Validates required IPv4/mask and optional gateway/DNS values without throwing
  for ordinary invalid input. Rejects IPv6, prohibited ranges, non-contiguous
  masks, conventional network/broadcast hosts, off-subnet gateways, gateways
  equal to the requested host address and secondary-only DNS.
- Produces canonical dotted-decimal values and `null` for absent optionals.
  `/31` endpoints and `/32` hosts are explicitly accepted because they have no
  conventional network/broadcast host values.
- Compares IPv4, mask, gateway, both DNS fields and static/DHCP mode, returning a
  flags-based field difference set and equivalence/no-change result.
- Reads the exact `NetworkAdapterId` afresh, then returns typed Ready, NoChange,
  ValidationFailed, AdapterUnavailable, MultipleIpv4RequiresSafetyDecision,
  PotentialAddressConflict, ProbeIndeterminate, Cancelled or AdapterReadFailed
  outcomes.
- No-change takes precedence over conflict probing. Multiple IPv4 assignments
  stop at a safety decision. A positive ICMP response is only a potential
  conflict; silence remains explicitly inconclusive.

## Architecture compliance

- No code calls Windows configuration mutation APIs and no Apply/DHCP UI was
  added.
- Domain stays Windows/WPF independent; Application owns rules/orchestration;
  Infrastructure owns `Ping`; App remains the composition root.
- Preflight calls the existing asynchronous reader by stable adapter identity on
  every invocation and never substitutes a name-matched adapter.
- Conflict probing is bounded, non-aggressive and best effort. Expected adapter
  read and probe failures are typed, while unexpected programming defects remain
  visible to critical error handling. No-response never means definitely free.

## Tests added

- Private/public/APIPA input, malformed/IPv6/prohibited IPv4 input.
- Contiguous/non-contiguous masks and `/0`, `/31`, `/32` boundaries.
- Gateway and DNS optional/error combinations, self-gateway rejection including
  `/31` and `/32`, `/31` peer gateway acceptance, and subnet endpoint math.
- Canonical/whitespace equivalence and every comparison field.
- Fresh identity read, disappearance, invalid/no-change short-circuits,
  additional IPv4 safety and every conflict-probe outcome.

## New dependencies

`None`

## Known limitations

1. Sprint 06 uses ICMP echo only; firewalls and silent hosts make a negative
   result inconclusive by design.
2. Adapters with additional IPv4 assignments cannot proceed past the typed
   safety condition until Sprint 07 mutation semantics are approved.
3. Preflight is registered but intentionally not connected to an Apply/DHCP UI.

## Questions for architect

None for Sprint 06. Multiple-IPv4 mutation behavior remains an explicitly
deferred Sprint 07 architecture decision.

## Do not continue

Sprint 07 was not started.
