# IPMan Codex Workflow

IPMan uses this repository workflow:

`TASK.md -> Codex -> RESULT.md + REVIEW.patch/evidence -> Architect -> next TASK.md`

The current task is explicitly supplied. Codex implements and validates it,
writes the architect handoff and review artifacts, then stops for architect
review.

Transient task, result, review, and evidence files live under
`artifacts/codex/`. The repository's existing `artifacts/` rule keeps these files
out of Git.

Persistent repository rules live in root `AGENTS.md` and `.codex/`.

Historical `.claude` files and sprint documents remain available as project
history, reference material, and specification. They are not automatically
active instructions; the current task must explicitly reference any that are
needed.
