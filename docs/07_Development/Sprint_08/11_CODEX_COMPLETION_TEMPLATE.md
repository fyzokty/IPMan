# Sprint 08 Completion Report

## Build

- Debug: PASS — `dotnet build IPMan.sln`
- Release: PASS — `dotnet build IPMan.sln -c Release`
- Warnings: 0
- Errors: 0

## Default tests

- Total: 337
- Passed: 336
- Failed: 0
- Skipped: 1 (`NOT EXECUTED` destructive scenario)

Confirmed: `Normal test execution did not mutate any real network adapter.`
The destructive scenario was reported as skipped / `NOT EXECUTED` because no
opt-in values were present.

## Integration harness

- Project/location: `tests/IPMan.IntegrationTests`
- Default destructive behavior: skipped as `NOT EXECUTED` before adapter
  discovery or mutation.
- Required opt-ins: explicit enable, exact GUID, isolated/disposable
  acknowledgement, destructive-filter acknowledgement, conflict-risk
  acknowledgement, one scenario, and complete IPv4/mask/gateway/DNS values.
- Exact adapter selection mechanism: exact, unique, case-insensitive interface
  GUID match in the independent observer; production reader plus WMI `SettingID`
  recovery resolution must return that same GUID. No name/index/first-adapter
  fallback exists.
- Evidence output location:
  `artifacts/integration/sprint08/<UTC timestamp>-<run ID>/` (git-ignored).

## Real isolated run status

`NOT EXECUTED — no isolated adapter designated`

## Architect review corrections

- No-opt-in destructive discovery is represented as an xUnit skipped test.
- Focused scenario guards permit only the requested gateway or DNS dimension;
  unsafe unrepresentable gateway-metric preservation fails closed.
- Independent IPv6 evidence includes exact-interface `GetIpForwardTable2`
  routes and compares stable route configuration fields.
- Unknown/unsupported richer DNS payload versions or types make observation
  incomplete and prevent a non-interference PASS.
- `summary.sanitized.json` emits only stable failure code/summary and a
  difference count; raw exception and technical text stays in local
  `result.json`.

## Scenario results

| Scenario | Result | WMI codes | Verified actual state | Rollback checked |
|---|---|---|---|---|
| Static to different static | NOT EXECUTED | N/A | No | No |
| DHCP to static | NOT EXECUTED | N/A | No | No |
| Set gateway | NOT EXECUTED | N/A | No | No |
| Clear gateway | NOT EXECUTED | N/A | No | No |
| Manual DNS — one server | NOT EXECUTED | N/A | No | No |
| Manual DNS — two servers | NOT EXECUTED | N/A | No | No |
| Automatic/empty DNS | NOT EXECUTED | N/A | No | No |

Safety-block and verification-failure behavior remains covered by deterministic
default tests; these are not reported as real integration passes.

## IPv6 non-interference

- Executed: No
- Before/after result: Not available; no isolated real run occurred.
- Unexpected change: Not assessed.
- Route observation implementation: Ready; real before/after route evidence not
  executed.

## DNS / DoH

- DNS mode tested: No real test.
- Manual DNS tested: No.
- Automatic DNS tested: No.
- DoH state available: Not assessed.
- DoH case executed: No — `DoH integration case not executed`.
- Unexpected unrelated DNS-property change: Not assessed.

## Evidence

- Run/evidence directory: None; real run not executed.
- Sanitized summary file: None; real run not executed.
- Rollback snapshot IDs: None.

## Git

- `git status --short`: the existing Sprint 08 working tree remains present;
  architect review documents/patch and seven new review-fix test/harness files
  are untracked. No files were staged or committed by this review-fix pass.
- `git diff --stat`: 47 files changed, 3,656 insertions, 62 deletions; untracked
  review-fix files are not included in this stat.

## New dependencies

None. The integration project reuses repository-pinned test/logging packages.

## Blockers / architect questions

An operator has not designated a disposable isolated adapter or supplied the
explicit real-run scenario values. Therefore AC-S08-009 through AC-S08-019 that
require actual Windows mutation evidence remain pending architect-supervised
real runs.

## Gate recommendation

`HARNESS_READY_REAL_RUN_PENDING`

Final gate approval belongs to architect.

## Do not continue

Do not wire production Apply.
Do not begin Sprint 09.
Do not commit before architect review.
