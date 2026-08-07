# IPMan — Codex Control Pack

This package adds Codex-specific repository instructions without replacing the
existing product, architecture, sprint or Claude-oriented documentation.

The authoritative project documentation remains under:

- `.claude/`
- `docs/`
- `docs/ADR/`

Codex receives its repository-level working rules from the root `AGENTS.md`.

## Installation

Extract this package over the existing IPMan repository.

Expected result:

- `IPMan/AGENTS.md`
- `IPMan/.codex/CODEX_WORKFLOW.md`
- `IPMan/.codex/CODE_REVIEW_HANDOFF.md`
- `IPMan/docs/07_Development/Sprint_06/11_PROMPT_TO_CODEX.md`
- `IPMan/docs/Templates/CODEX_SPRINT_PROMPT_TEMPLATE.md`

## Important

Codex must not commit changes unless the user explicitly asks it to.

This is intentional: the architect review workflow depends on inspecting the
working-tree diff before the user creates the sprint commit.
