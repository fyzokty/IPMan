---
title: Sprint 07 Architect Review Checklist
version: 1.0.0
status: Approved
---

# Safety

- [ ] Full gateway collection preserved.
- [ ] Full DNS collection preserved.
- [ ] Extra addresses/gateways/DNS cannot be silently erased.
- [ ] Rollback captured before mutation.
- [ ] Rollback failure blocks mutation.
- [ ] Adapter identity mapping cannot drift to another adapter.
- [ ] No shell fallback.

# WMI

- [ ] WMI calls isolated in Infrastructure.
- [ ] Return codes translated.
- [ ] Partial step results observable.
- [ ] Gateway-clear behavior based on documented semantics.
- [ ] DNS-empty behavior based on documented semantics.
- [ ] No undocumented "best guess" API usage.

# Apply orchestration

- [ ] Preflight first.
- [ ] NoChange no-op.
- [ ] Conflict requires explicit continuation.
- [ ] Full topology gates enforced.
- [ ] Fresh verification mandatory.
- [ ] WMI return code alone never reports product success.
- [ ] Bounded verification.
- [ ] Concurrent mutation prevented.

# Error handling

- [ ] Expected failures typed.
- [ ] Unexpected defects not swallowed.
- [ ] Partial failure not success.
- [ ] Rollback reference retained in failure result.

# Tests

- [ ] Default tests non-destructive.
- [ ] Build Debug/Release clean.
- [ ] All tests pass.
- [ ] No broad analyzer suppression.
