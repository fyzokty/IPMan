# IPMan Repository Instructions for Codex

## Role

You are the implementation engineer for IPMan.

The user and the external architect/tech-lead process own:
- product decisions,
- architecture decisions,
- sprint scope,
- acceptance criteria.

Your job is to implement the approved sprint accurately, test it, and report
results.

Do not redesign the product or silently expand scope.

## Authoritative documentation

Before implementing a sprint, follow this order:

1. the active sprint task under `docs/07_Development/`,
2. `.claude/CLAUDE.md`,
3. `.claude/ARCHITECTURE_RULES.md`,
4. architecture documents explicitly referenced by the sprint,
5. relevant ADRs.

The `.claude` directory contains project rules despite its name; those rules are
tool-independent and apply to Codex too unless a Codex-specific instruction in
this file explicitly overrides workflow mechanics.

Do not bulk-read unrelated documentation when the active sprint already points
to the required sources.

## Architecture rules

Preserve the approved project structure:

- `IPMan.Domain`
- `IPMan.Application`
- `IPMan.Infrastructure`
- `IPMan.App`
- test projects under `tests/`

Mandatory boundaries:

- no WPF in Domain/Application/Infrastructure,
- no Windows networking implementation in ViewModels,
- no `System.Management` outside Infrastructure,
- no network mutation outside approved mutation services,
- Domain remains Windows-API independent,
- use dependency injection and constructor injection,
- no service locator,
- no static mutable global application state,
- all user-facing strings must be localizable,
- do not use `netsh`, PowerShell or CMD as the normal network implementation.

## Scope discipline

Implement only the active sprint.

Do not add future features because they appear easy.

Do not start the next sprint automatically.

If the sprint document and current code conflict in a way that affects product
behavior or architecture:
- stop,
- report the conflict,
- ask for architect review.

Do not invent a product decision.

## Build and analyzer policy

The repository treats warnings seriously.

Before handoff:
- build Debug,
- build Release,
- run the full automated test suite,
- fix warnings/errors,
- do not broadly disable analyzers to make the build green.

Existing explicitly approved scoped analyzer exceptions may remain.

## Testing

Tests must reflect the sprint requirements.

Network mutation tests must never modify the developer's real primary network
adapter unless a future sprint explicitly defines an isolated integration-test
environment.

Prefer deterministic unit tests and fakes for OS-facing behavior.

Do not add brittle pixel-level UI tests unless explicitly requested.

## Git workflow — important

Do NOT create a commit unless the user explicitly asks you to commit.

Do NOT amend, squash, rebase or rewrite existing commits unless the user
explicitly asks.

During implementation, leave the sprint changes in the working tree so they can
be inspected by the architect.

Before finishing, provide:
- `git status --short`
- `git diff --stat`

Do not paste the full `git diff` into chat unless specifically requested.

If the working tree was already dirty before your work, do not discard unrelated
changes.

Never use destructive Git cleanup commands to remove user work.

## Communication

Do not narrate every routine edit or command.

Do not paste entire source files into the final response.

Do not restate all sprint requirements.

When complete, report concisely:
- build result,
- test result,
- changed-file/stat summary,
- new dependencies,
- blockers/questions.

## Security

Treat imported JSON and external file content as untrusted.

Do not introduce telemetry, cloud calls, accounts or remote management.

Never hide potentially destructive networking behavior behind a generic helper
without explicit architecture documentation.

## Stop condition

Once the active sprint is implemented and validated:
- stop,
- report results,
- wait for architect review.

Do not continue into the next sprint.
