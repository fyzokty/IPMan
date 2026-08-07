# Codex Task — Sprint XX

Implement the approved **Sprint XX — <name>**.

## Read before coding

1. `/AGENTS.md`
2. `.claude/CLAUDE.md`
3. `.claude/ARCHITECTURE_RULES.md`
4. architecture documents explicitly listed by this sprint
5. relevant ADRs
6. every specification file in `docs/07_Development/Sprint_XX/`

## Objective

<one-paragraph sprint objective>

## Implement

- <approved item>
- <approved item>
- <approved item>

## Do not implement

- <future/out-of-scope item>
- <future/out-of-scope item>

Do not redesign architecture or invent product behavior.

## Validation

Run:

`dotnet build IPMan.sln`

`dotnet build IPMan.sln -c Release`

`dotnet test IPMan.sln`

Fix all warnings/errors.

## Git/review workflow

Do not commit unless explicitly asked.

Provide:
- `git status --short`
- `git diff --stat`

Do not paste full diffs or source files unless requested.

## Completion

Report:
- build,
- tests,
- diff stat,
- new dependencies,
- blockers/questions.

Stop. Do not start the next sprint.
