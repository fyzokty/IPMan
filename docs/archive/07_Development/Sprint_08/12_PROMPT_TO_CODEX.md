# Codex — Sprint 08 Isolated Windows Mutation Validation

Implement Sprint 08 according to every document in this directory.

Read:
- `/AGENTS.md`
- `.claude/CLAUDE.md`
- `.claude/ARCHITECTURE_RULES.md`
- Sprint 07 architecture/review docs
- current Sprint 07 implementation
- every file under `docs/07_Development/Sprint_08/`

# Primary task

Create the safe opt-in destructive integration harness and evidence capture
needed to validate the Sprint 07 production mutation engine.

# Safety

Do NOT mutate any adapter merely because it looks virtual/disconnected/safe.

Do NOT select the first adapter.

Do NOT run destructive tests unless the operator explicitly supplies:
- enable opt-in,
- exact adapter GUID,
- isolated/disposable acknowledgement,
- required test configuration.

If those values are not already explicitly available, implement and test the
harness but DO NOT perform a real mutation.

# Microsoft documentation

Use Microsoft Learn as the primary source for Windows API/WMI behavior.

Relevant documented areas include:
- Win32_NetworkAdapterConfiguration.EnableStatic
- SetGateways
- SetDNSServerSearchOrder
- GetInterfaceDnsSettings
- DNS_INTERFACE_SETTINGS / EX / SETTINGS3
- FreeInterfaceDnsSettings

Do not guess undocumented behavior.

# Validation

Run:
- `dotnet build IPMan.sln`
- `dotnet build IPMan.sln -c Release`
- `dotnet test IPMan.sln`

The default suite must remain non-destructive.

Complete:
`11_CODEX_COMPLETION_TEMPLATE.md`

If no isolated adapter has been explicitly designated:
- report `HARNESS_READY_REAL_RUN_PENDING` if the harness is otherwise ready,
- do not ask the code to choose an adapter itself.

Do not wire Apply.
Do not begin Sprint 09.
Do not commit.
