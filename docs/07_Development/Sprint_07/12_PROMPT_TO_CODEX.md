# Codex — Sprint 07 Static IPv4 Mutation + Rollback Capture

Implement the approved Sprint 07.

Read:

1. `/AGENTS.md`
2. `.claude/CLAUDE.md`
3. `.claude/ARCHITECTURE_RULES.md`
4. the architecture docs referenced by `00_CODEX_TASK.md`
5. relevant ADRs
6. every file in `docs/07_Development/Sprint_07/`

Inspect the existing Sprint 06 implementation before editing.

Implement Sprint 07 only.

Important:
- this sprint may implement real WMI mutation code,
- do NOT wire it to the production Apply button,
- default automated tests must remain non-destructive,
- do not use netsh/PowerShell/CMD,
- do not guess undocumented gateway/DNS clearing behavior.

If Microsoft-documented WMI behavior cannot safely satisfy the specified
gateway-empty or DNS-empty semantics, stop that concrete path and report it as an
architect blocker instead of creating a shell workaround.

Run:

- `dotnet build IPMan.sln`
- `dotnet build IPMan.sln -c Release`
- `dotnet test IPMan.sln`

Fix all warnings/errors.

Complete:
`11_CODEX_COMPLETION_TEMPLATE.md`

Do not commit.
Do not start Sprint 08.
