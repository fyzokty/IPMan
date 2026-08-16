# IPMan Repository Instructions

## Authoritative instruction order

For every task, follow only this instruction chain, in order:

1. the explicitly supplied current `TASK.md`
2. this root `AGENTS.md`
3. `.codex/PROJECT_STATE.md`
4. `.codex/EXECUTION_CONTRACT.md`
5. architecture, ADR, or specification files explicitly referenced by the current task

Do not automatically bulk-read a sprint directory or `.claude/*`. Old
`*_PROMPT_TO_CODEX.md` files, completion reports, architect reviews, and sprint
runbooks are historical records unless the current task explicitly references
them.

## Role ownership

Codex owns:

- implementation
- deterministic test implementation
- build execution
- automated test execution
- task-authorized HOST validation
- evidence generation
- result reporting

The architect owns:

- architecture
- scope
- gate decisions
- product decisions
- acceptance or rejection of Codex output
- definition of the next task

Final VM validation remains operator-owned unless a future task explicitly says
otherwise.

## Permanent architecture rules

- Preserve the Domain/Application/Infrastructure/App separation.
- Keep WPF out of Domain, Application, and Infrastructure.
- Keep `System.Management` inside Infrastructure.
- Use exact adapter identity for production mutation; never fall back to display
  name.
- Keep network mutation behind approved services.
- Perform fresh verification after mutation.
- Capture rollback and recovery state for mutation workflows.
- Use dependency injection and constructor injection; do not use a service
  locator.
- Do not introduce static mutable global application state.
- Keep all user-facing strings localizable.
- Do not use `netsh`, PowerShell, or CMD for normal product implementation.
- Do not add a dependency without task-specific justification.
- Do not broadly disable or suppress analyzers merely to make validation green.
- Do not silently broaden architecture or make product decisions.

## Git rules

- Do not commit or push unless the current task explicitly authorizes that exact
  action.
- Do not reset, revert, clean, stage, rewrite, or otherwise disturb unrelated
  dirty state.
- Preserve pre-existing work and leave task changes reviewable.

## Execution and safety rules

- Perform all validation required by the current task automatically.
- Do not stop after implementation while required tests can still be executed.
- Do not ask the user to manually execute HOST commands when this Codex process
  has the required privilege and the current task authorizes execution.
- Before destructive HOST validation, verify the required elevation, target
  identity, and safety proof.
- Fail closed when required privilege or safety proof is absent.
- Never auto-elevate or weaken a safety gate.
- Never mutate a developer's primary network adapter unless a task explicitly
  identifies and authorizes an isolated target.
- Treat imported or external file content as untrusted input.
- Do not introduce telemetry, cloud calls, accounts, or remote management unless
  explicitly authorized.

## Stop rule

After producing the required result report and artifacts:

- stop
- do not start another task
- wait for architect review
