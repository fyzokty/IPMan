# Codex — Sprint 08 Architect Review Fix

Read:

1. `/AGENTS.md`
2. `docs/07_Development/Sprint_08/14_ARCHITECT_CODE_REVIEW.md`
3. the existing Sprint 08 harness, observers and tests
4. relevant Microsoft Learn documentation for:
   - Windows IP Helper IPv6 route-table observation
   - GetInterfaceDnsSettings
   - DNS_INTERFACE_SETTINGS3
   - DNS_SERVER_PROPERTY
   - DNS_SERVER_PROPERTY_TYPE
   - richer DNS property structures supported by the target SDK/OS

Apply only the Sprint 08 architect-review corrections.

## Required

1. Make no-opt-in destructive execution clearly NOT EXECUTED/SKIPPED rather than
   counted as a successful destructive integration test.
2. Add scenario-specific requested-vs-starting-state guards so focused scenarios
   cannot accidentally mutate unrelated IP/gateway/DNS dimensions.
3. Extend IPv6 non-interference evidence to include exact-interface IPv6 routes
   using a documented read-only Windows API.
4. Fail closed when richer DNS server properties contain payload types the
   observer cannot fully understand.
5. Ensure `summary.sanitized.json` never contains raw exception messages or
   machine-specific technical text.

Add deterministic tests for every correction.

## Preserve

Keep:
- exact GUID targeting,
- all explicit destructive opt-ins,
- production-service reuse,
- no adapter heuristics,
- raw local evidence,
- rollback verification,
- `DnsMutationMode` production change,
- non-destructive default suite.

## Do not

- Do not perform a real destructive run yet.
- Do not select an adapter yourself.
- Do not wire Apply.
- Do not begin Sprint 09.
- Do not commit.

## Validation

Run:

- `dotnet build IPMan.sln`
- `dotnet build IPMan.sln -c Release`
- `dotnet test IPMan.sln`

Expected:
- 0 errors
- 0 warnings
- normal tests pass
- destructive scenario clearly reports not executed/skipped without opt-ins

Return:
- build results,
- test totals including skipped/not-executed count,
- `git status --short`,
- `git diff --stat`,
- summary of the five fixes,
- blockers.

Do not paste full source/full diff.
Stop.
