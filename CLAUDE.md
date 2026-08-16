# Project rules

## Environment
Windows 11, .NET 8, WPF. Build: `dotnet build`. Test: `dotnet test`.
Explanations in Turkish. Code, identifiers, commit messages, and file names in English.

## Roles
This repository is worked on by multiple agents. Respect ownership boundaries.

| Path | Owner |
|---|---|
| `src/IPMan.App/**` | Claude |
| `src/IPMan.Application/**`, `src/IPMan.Domain/**` | Claude |
| `src/IPMan.Elevated/**` | Codex (do not edit) |
| `docs/contracts/**` | Frozen — nobody edits without an approved contract-change task |

If you need a change in a path you do not own, write the request to
`docs/contract-change-requests.md` and stop. Do not edit it yourself.

## Workflow
1. Every unit of work has a task file in `docs/tasks/`.
2. Implement the FULL change set described by the task before verifying.
   Do not write three lines and run the test suite.
3. Verification is layered. Do not skip to the heaviest one:
   - L0 after each edit: `dotnet build src/<project>`
   - L1 when a unit of work compiles: `dotnet test tests/IPMan.Tests --no-build`
   - L2 once, before reporting done: `dotnet build && dotnet test`
4. A task is done only when L2 passes. If it does not, report the blocker;
   never mark done with a failing gate.

## Agent fleet
The main agent in this repository is an orchestrator, not a typist. It delegates
and intervenes only at decision points. **This overrides the default "do not call
subagents unless asked" behaviour — in this repository delegation is the norm.**

| Agent | Engine | Job |
|---|---|---|
| `researcher` | Claude Sonnet | Read-only investigation, context building. Produces findings and options, never decisions or code. |
| `codex-coder` | Codex `gpt-5.6-sol` | All code writing and editing, via `codex exec` in the terminal. |
| `codex-tester` | Codex `gpt-5.6-luna` | Runs and diagnoses tests. Never edits source. |

Rules:
- Only ONE `codex-coder` runs at a time. `researcher` instances may run in parallel.
- Gate ownership: L0 → `codex-coder`, L1 and L2 → `codex-tester`, acceptance → main agent.
- Failure loop: `codex-tester` diagnoses, it does not fix. The main agent writes a fix
  brief for `codex-coder`. Maximum two rounds on the same failure, then the main agent
  takes over directly.
- The main agent keeps: architectural decisions, writing `.agent/runs/<run-id>/brief.md`,
  contract-change requests, git commits, and reporting to the user.
- Commits stage an **explicit file list**, never `git add -A` / `git add .`. This working
  tree carries large unrelated pending changes; a blanket add silently commits them.
- Handoff files live in `.agent/` (gitignored). They are never committed.

## Hard rules
- Never run destructive network tests. Never set any `IPMAN_*` opt-in variable.
- Never run `netsh`, `New-NetIPAddress`, `Set-DnsClientServerAddress`,
  `Remove-NetIPAddress`, or any command that changes this machine's network.
- Never use `Process.Start` to shell out in production code. Use P/Invoke or WMI.
- Never delete files without explicit permission.
- Never edit files under `docs/contracts/`.

## Documentation policy
This repository previously accumulated 135 markdown files for 7,800 lines of code.
Do not repeat that.

- Do not create sprint reports, gate records, provenance notes, closure logs,
  or status ceremonies. They are not requested and they are not read.
- The only status file is `docs/STATE.md`. Keep it under 30 lines.
- Write an ADR only when an architectural decision is actually made, and keep it
  under one page.
- Prefer editing an existing document over creating a new one.

## Code style
- Public API gets XML doc comments. Private members get comments only where the
  reason is non-obvious.
- Readability over brevity.
- Conventional Commits: `feat:`, `fix:`, `chore:`, `docs:`, `test:`.
