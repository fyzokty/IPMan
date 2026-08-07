# Codex -> Architect Handoff Format

Use this format at the end of a sprint.

## Build

- Debug:
- Release:
- Warnings:
- Errors:

## Tests

- Total:
- Passed:
- Failed:
- Skipped:

## Git

Paste:

`git status --short`

and:

`git diff --stat`

Do not paste the full diff unless requested.

## New dependencies

List package/dependency changes and why.

If none:

`None`

## Scope/architecture notes

Only mention:
- a requirement that could not be implemented,
- an architecture conflict,
- a known safety limitation,
- a decision that requires architect/product approval.

If none:

`None`

## Stop

Do not commit.
Do not start the next sprint.
