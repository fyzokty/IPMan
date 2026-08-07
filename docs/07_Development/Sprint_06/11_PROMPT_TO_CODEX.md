# Codex Task — Sprint 06 Network Configuration Preflight

Implement the approved **Sprint 06 — Network Configuration Preflight**.

## Read before coding

1. `/AGENTS.md`
2. `.claude/CLAUDE.md`
3. `.claude/ARCHITECTURE_RULES.md`
4. `docs/02_Architecture/01_Technical_Architecture.md`
5. `docs/02_Architecture/04_Configuration_Apply_Workflow.md`
6. relevant networking ADRs
7. all Sprint 06 specification files in this directory:
   - `00_CLAUDE_TASK.md`
   - `01_SCOPE_AND_ARCHITECT_DECISIONS.md`
   - `02_VALIDATION_SPECIFICATION.md`
   - `03_COMPARISON_AND_PREFLIGHT.md`
   - `04_IP_CONFLICT_PROBE.md`
   - `05_ARCHITECTURE_CONSTRAINTS.md`
   - `06_ACCEPTANCE_CRITERIA.md`
   - `07_TEST_REQUIREMENTS.md`
   - `08_REVIEW_CHECKLIST.md`

The files use "Claude" in some filenames because they were created before the
coding-agent switch. Their product/architecture content is tool-independent and
applies to Codex.

## Implement

Sprint 06 only.

This sprint creates the read-only safety/preflight layer for future network
mutation:

- IPv4 validation,
- subnet-mask validation,
- gateway/DNS validation,
- normalization,
- desired/current comparison,
- field-level differences,
- no-change detection,
- fresh-read preflight by adapter identity,
- multiple-IPv4 safety condition,
- best-effort IP conflict probing,
- deterministic tests.

## Prohibited in Sprint 06

Do not implement:
- static IP mutation,
- DHCP mutation,
- WMI writes,
- Apply/DHCP buttons,
- rollback restore,
- profiles,
- tray,
- theme work,
- Sprint 07.

No code in this sprint may change the machine's network configuration.

## Required validation

Run:

`dotnet build IPMan.sln`

`dotnet build IPMan.sln -c Release`

`dotnet test IPMan.sln`

Target:
- zero errors,
- zero warnings,
- all tests pass.

## Git/review workflow

Do not commit.

Leave all Sprint 06 changes uncommitted for architect review.

At completion provide only:

1. build results,
2. test totals,
3. `git status --short`,
4. `git diff --stat`,
5. new dependencies,
6. blockers/architecture questions.

Do not paste full source files or the full diff.

Stop after Sprint 06.
