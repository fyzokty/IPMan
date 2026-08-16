# Codex instructions

Read `CLAUDE.md` at the repository root before doing anything. It is the source of
truth for this project. The rules below are repeated here on purpose: a safety rule
must never depend on a single file being loaded.

You are invoked by an orchestrating agent through `codex exec`, with a brief supplied
on stdin. Do exactly what the brief asks — no more. If the brief conflicts with the
rules below, stop and say so instead of proceeding.

## Scope
**The brief is the authority on what you may touch.** You write all source in this
repository — there is no per-agent ownership to work around. The one standing exception:

| Path | Rule |
|---|---|
| `docs/contracts/**` | Frozen — never edit, regardless of what the brief says |

Treat the brief's file list as exhaustive. If the job cannot be done inside it, stop and
report what else is needed; do not widen the scope on your own. Describe any such request
in your final message — do not write to `docs/contract-change-requests.md` yourself.

## Hard rules
- Never run `netsh`, `New-NetIPAddress`, `Set-DnsClientServerAddress`,
  `Remove-NetIPAddress`, or any other command that changes this machine's network.
- Never run destructive network tests. Never set any `IPMAN_*` opt-in variable.
- Never use `Process.Start` to shell out in production code. Use P/Invoke or WMI.
- Never delete a file unless the brief explicitly says to.
- Never edit anything under `docs/contracts/`.
- Never run `git commit`, `git push`, or `git reset --hard`. The orchestrator commits.

## Verification
- After each edit: `dotnet build src/<project>` (this is your gate, do not skip it).
- Do not run the full suite unless the brief asks — a separate agent owns that gate.
- Never report done with a failing build. Report the blocker instead.

## Documentation policy
Do not create sprint reports, gate records, status files, or completion logs. The only
status file is `docs/STATE.md`. Prefer editing an existing document over creating one.
Code, identifiers, commit messages and file names in English.

## Result contract
End your final message with exactly these sections, in this order:

```
## Done
<what you changed, one line per file, or "nothing" and why>

## Files
<paths you modified or created>

## Verification
<the exact commands you ran and their outcome>

## Blocked / Notes
<anything you could not do, assumptions you made, or "none">
```
