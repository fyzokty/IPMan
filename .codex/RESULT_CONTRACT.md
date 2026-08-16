# Codex Result Contract

Every task result must use this structure.

# <TASK_ID> Result

## Outcome

Use exactly one:

- `PASS_CANDIDATE`
- `CHANGES_REQUIRED`
- `BLOCKED`
- `FAIL_SAFE`

Codex does not issue final architect approval.

## Summary

Maximum five concise bullets.

## Changed Files

List tracked files changed by this task. If none, write `None`.

## Implementation / Fix

Concise description of the actual implementation.

## Validation

Include exact results for:

- focused tests
- HOST self-tests, if applicable
- Debug build
- Release build
- unit tests
- integration tests
- skipped tests
- `git diff --check`

Use `NOT APPLICABLE` only when genuinely irrelevant or explicitly excluded by
the task.

## Live HOST Validation

When applicable, include:

- elevation status
- exact target GUID
- safety gate
- authorized mutation results
- rollback result
- final equality

Otherwise write `NOT APPLICABLE`.

## Safety

State:

- unexpected mutation: yes/no
- rollback required/performed
- VM operation: yes/no
- other-adapter mutation: yes/no

## Git

Include:

- HEAD
- `git status --short`
- `git diff --stat`
- task-start baseline captured: yes/no
- destructive/live environment cleanup verified: yes/no/not applicable
- commit/push performed: yes/no

## Review Artifact

Include:

- artifact path, SHA256, line count, and byte count, or why it was not produced
- review patch scope: `TASK_ONLY` or `COMBINED_BASELINE_LIMITATION`

## Evidence

Include evidence paths and ZIP hash when applicable.

## Blockers

List only actual unresolved blockers. If none, write `None`.

## Architect Decision Requested

One concise sentence describing what the architect should review or decide next.
