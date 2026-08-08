# Codex — Sprint 08 Final Architect Review Fix

Read:

1. `/AGENTS.md`
2. `docs/07_Development/Sprint_08/16_ARCHITECT_FINAL_REVIEW.md`
3. existing Sprint 07/08 mutation and integration code
4. Microsoft Learn documentation for:
   - GetInterfaceDnsSettings
   - DNS_INTERFACE_SETTINGS / EX / SETTINGS3
   - DNS_SERVER_PROPERTY_TYPE
   - SetGateways / GatewayCostMetric behavior

Apply only these three final corrections:

1. Base DNS non-interference allowances on actual requested dimensions rather
   than broad scenario name.
2. Preserve the existing gateway metric whenever the gateway address is
   unchanged and gateway was not requested as a changed dimension; do not
   silently force metric 1.
3. Make DNS observer version selection capability-aware and fail closed on an
   unexpected failure of the highest platform-supported settings version.

Keep all five fixes from the previous review intact.

Add deterministic tests for every required case.

Do NOT:
- run a real destructive network test,
- wire production Apply,
- begin Sprint 09,
- commit.

Run:

- `dotnet build IPMan.sln`
- `dotnet build IPMan.sln -c Release`
- `dotnet test IPMan.sln`

Return:
- Debug/Release result,
- tests passed/failed/skipped,
- `git status --short`,
- `git diff --stat`,
- gateway metric preservation design,
- DNS dimension-aware comparison design,
- DNS version/capability selection design,
- blockers.

Stop.
