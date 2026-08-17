# Prompt to give Claude Code

Implement **Sprint 04 — Adapter Discovery** for IPMan.

First read and obey:

- `.claude/CLAUDE.md`
- `.claude/ARCHITECTURE_RULES.md`
- the approved product requirements
- the approved technical architecture and ADRs
- every document in `docs/07_Development/Sprint_04/`

Do not redesign the architecture.

Do not implement anything outside Sprint 04.

This sprint is strictly read-only network adapter discovery, network change
monitoring, minimal integration, and tests.

Do not implement static IP changes, DHCP changes, profiles, rollback persistence,
tray behavior, notifications, quick actions, or final UI.

Before changing code:
1. inspect the existing solution,
2. explain briefly which files/components you intend to change,
3. then implement.

After implementation:
1. restore packages if needed,
2. build the entire solution,
3. run tests,
4. fix all warnings/errors,
5. complete `06_CLAUDE_COMPLETION_TEMPLATE.md`,
6. stop.

Do not suppress analyzer rules to make the build green unless an existing
approved architecture rule explicitly requires it.
