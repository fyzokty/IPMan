# IPMan Codex Workflow

## 1. Start of sprint

Read the active `*_PROMPT_TO_CODEX.md` file.

Then inspect:
- `git status --short`
- current solution/project structure
- only the source files relevant to the sprint

Read the specific architecture/ADR references named by the sprint.

## 2. Implementation

Make changes directly in the existing repository.

Keep changes small enough to review.

Do not create parallel replacement architectures.

When an existing type/service can be evolved cleanly, prefer that over adding a
duplicate abstraction.

## 3. Validation

Run the commands required by the active sprint.

Unless the sprint says otherwise, minimum validation is:

`dotnet build IPMan.sln`

`dotnet build IPMan.sln -c Release`

`dotnet test IPMan.sln`

Fix all new warnings/errors.

## 4. Handoff

Do not commit.

Provide:
- build/test counts,
- `git status --short`,
- `git diff --stat`,
- any new dependencies,
- unresolved architecture/product questions.

The user will send the report/diff to the architect for review.

## 5. After architect review

Apply only the requested corrections.

Re-run build/tests.

Again leave the corrections uncommitted unless the user explicitly requests a
commit.

## 6. Commit ownership

The normal IPMan workflow is:

Codex implements
-> architect reviews
-> Codex fixes
-> architect approves
-> user commits

This keeps sprint diffs available for code review.
