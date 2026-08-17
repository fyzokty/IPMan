# Codex — Sprint 07 Architect Review Fix

Read:

1. `/AGENTS.md`
2. `docs/07_Development/Sprint_07/13_ARCHITECT_CODE_REVIEW.md`
3. the existing Sprint 07 implementation/tests
4. Microsoft Learn documentation relevant to:
   - EnableStatic
   - SetGateways
   - SetDNSServerSearchOrder
   - Win32_NetworkAdapterConfiguration properties needed for rollback fidelity

Apply only the architect-review corrections.

## Required corrections

1. Handle EnableStatic code 81 with method-specific and prior-state-aware
   semantics. Do not globally treat 81 as success.
2. Perform a second exact-identity fresh read immediately before rollback
   capture; re-run comparison/topology safety and use that snapshot for rollback
   and the mutation plan.
3. Replace the private StaticIpv4ApplyService semaphore with a shared injectable
   Application-layer mutation coordinator registered as singleton.
4. Make rollback capture restore-capable for the fields this sprint mutates:
   investigate/model DNS source semantics and gateway metric. Do not guess.
   If reliable Windows state cannot be obtained, stop and report a blocker.

## Do not do

- Do not wire the production Apply button.
- Do not implement DHCP mutation.
- Do not implement rollback restore yet.
- Do not implement profiles/theme/tray.
- Do not run destructive tests on the normal development adapter.
- Do not begin Sprint 08.
- Do not commit.

## Tests

Add/adjust deterministic tests for:
- EnableStatic 81 on already-static adapter,
- 81 not globally successful,
- second fresh-read drift handling,
- rollback sourced from second fresh state,
- shared mutation gate serialization,
- newly captured recovery semantics.

Run:
- `dotnet build IPMan.sln`
- `dotnet build IPMan.sln -c Release`
- `dotnet test IPMan.sln`

Target:
- 0 warnings
- 0 errors
- all tests pass.

## Completion report

Return:
- Debug/Release result,
- test total/pass/fail/skip,
- `git status --short`,
- `git diff --stat`,
- exact solution for code 81,
- exact solution for fresh pre-mutation state,
- exact solution for shared mutation coordination,
- exact rollback fields/source used for DNS mode and gateway metric,
- blockers, if any.

Do not paste full source or full diff.
Stop.
