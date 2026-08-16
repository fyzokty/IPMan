# Codex Execution Contract

This lifecycle is mandatory for every Codex task.

## Start

1. Read the explicitly supplied current `TASK.md`.
2. Read root `AGENTS.md`.
3. Read `.codex/PROJECT_STATE.md`.
4. Apply this `.codex/EXECUTION_CONTRACT.md`.
5. Capture the task-start baseline described below.
6. Read only task-referenced architecture, ADR, or specification files needed
   for implementation.

Do not activate historical prompts, reports, reviews, runbooks, or an entire
sprint directory unless the current task explicitly references them.

### Task-start dirty-tree baseline

Before editing, capture a lightweight baseline under:

`artifacts/codex/evidence/<TASK_ID>/task-start/`

Record at minimum:

- HEAD
- `git status --short`
- `git diff --stat`
- task-scope-relevant untracked files
- hashes, copies, or another deterministic baseline representation of files
  inside the task's Allowed Scope that were already dirty

The baseline must distinguish pre-existing intentional changes from current-task
changes without staging or otherwise modifying the Git index. Preserve enough
before-state information to prove a task-only review delta whenever technically
possible.

## Implement

- Stay within task scope.
- Preserve unrelated dirty work.
- Make the smallest coherent change.
- Add regression coverage for defects.
- Do not silently broaden architecture.

## Validate automatically

Unless the task explicitly narrows validation, standard code-change validation
is:

- relevant focused tests
- HOST lab self-tests when `tools/host-lab` is affected
- `dotnet build IPMan.sln`
- `dotnet build IPMan.sln -c Release`
- `dotnet test IPMan.sln`
- `git diff --check`

`dotnet test IPMan.sln` means the normal safety-gated suite by default.
Destructive or live integration scenarios must remain skipped or disabled unless
the current task explicitly authorizes the exact live action and every required
elevation, target-identity, and safety gate passes. Running Codex as
Administrator does not itself authorize destructive tests or live HOST mutation.

### Persistent elevated-session environment isolation

Administrator or elevated Codex sessions must establish a safe default
environment before normal build or test execution, with destructive and live
HOST authorization disabled.

Task-specific environment variables that enable or acknowledge destructive
integration tests, live HOST mutation, exact adapter targeting, or
recovery/mutation scenarios must:

- be scoped only to the exact child process or validation step that needs them
- be restored or removed immediately after that process or step
- never be persisted as machine- or user-level environment changes
- never flow into unrelated build/test commands or a later task

The current task must identify implementation-specific variable names when they
are needed; this permanent contract does not guess them. At task finish, verify
that no destructive/live authorization introduced by the task remains active.

If the task authorizes live HOST validation, Codex must:

- verify elevation and safety before mutation
- execute the authorized validation itself when the required privilege is
  available
- capture evidence
- perform the required rollback
- fail closed on safety uncertainty

VM validation is never automatic unless the task explicitly authorizes it.

## Report

Always create the primary architect handoff at:

`artifacts/codex/reports/<TASK_ID>_RESULT.md`

Follow `.codex/RESULT_CONTRACT.md`. Chat output is only a short pointer and status
summary.

## Review artifact

When tracked source, tests, or documentation changed, generate:

`artifacts/codex/review/<TASK_ID>_REVIEW.patch`

The patch must represent the task's reviewable changes while preserving the
pre-existing intentional working state. In a dirty working tree, generate a
task-only patch whenever technically possible by comparing the final files with
the captured task-start baseline, not merely with HEAD when that would include
pre-existing changes.

The result must report review patch scope as exactly one of:

- `TASK_ONLY`
- `COMBINED_BASELINE_LIMITATION`

If task-only isolation cannot be proven, do not claim the patch is isolated;
report `COMBINED_BASELINE_LIMITATION` and produce the safest available review
artifact.

## Evidence

Store live, destructive, or OS-facing validation evidence under:

`artifacts/codex/evidence/<TASK_ID>/`

An optional evidence archive may be written to:

`artifacts/codex/evidence/<TASK_ID>_EVIDENCE.zip`

## Finish

Unless the task explicitly authorizes the exact action, do not:

- commit
- push
- start the next task
- declare an architect gate `PASS`

Stop after producing the required report and artifacts, then wait for architect
review.
