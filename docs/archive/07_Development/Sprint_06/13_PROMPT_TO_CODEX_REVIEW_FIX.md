# Codex — Sprint 06 Architect Review Fix

Read:

1. `/AGENTS.md`
2. `docs/07_Development/Sprint_06/12_ARCHITECT_CODE_REVIEW.md`
3. the existing Sprint 06 implementation and tests

Apply only the two required corrections in the architect review:

1. reject a gateway equal to the requested host IPv4 address, including correct
   /31 and /32 tests;
2. remove the broad exception-swallowing behavior in preflight and replace it
   with narrow/typed expected-failure handling while allowing unexpected
   programming defects to reach critical error handling.

Do not implement the Sprint 07 architecture-gate items yet.

Do not implement network mutation.

After changes:

- build Debug,
- build Release,
- run all tests,
- fix all warnings/errors.

Do not commit.

Return:
- build result,
- test result,
- `git status --short`,
- `git diff --stat`,
- any blocker.

Stop.
