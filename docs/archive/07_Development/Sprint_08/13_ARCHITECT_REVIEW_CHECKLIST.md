---
title: Sprint 08 Architect Review Checklist
version: 1.0.0
status: Approved
---

# Harness safety

- [ ] Default `dotnet test` cannot mutate a real adapter.
- [ ] Explicit destructive opt-in required.
- [ ] Exact adapter GUID required.
- [ ] Isolated/disposable acknowledgement required.
- [ ] No name/index/virtual-adapter heuristic.
- [ ] Wrong/ambiguous target fails closed.

# Architecture

- [ ] Production apply/mutation services reused.
- [ ] Integration code does not duplicate product mutation logic.
- [ ] Evidence capture is separated from product runtime storage.
- [ ] Sensitive evidence is not committed.
- [ ] No shell mutation fallback.

# Real-run evidence

- [ ] Static->static.
- [ ] DHCP->static.
- [ ] Gateway set.
- [ ] Gateway clear.
- [ ] Manual DNS.
- [ ] Automatic/empty DNS.
- [ ] Actual WMI codes recorded.
- [ ] Rollback pre-state verified.

# Non-interference

- [ ] IPv6 before/after evidence.
- [ ] No unexplained IPv6 loss.
- [ ] DNS richer settings before/after evidence where supported.
- [ ] DoH executed or explicitly marked not executed.
- [ ] No unrelated DNS/DoH damage.

# Quality

- [ ] Debug clean.
- [ ] Release clean.
- [ ] Default tests pass.
- [ ] No real normal-adapter mutation.
- [ ] Gate recommendation is evidence-based.

# Gate

- [ ] NOT_READY
- [ ] HARNESS_READY_REAL_RUN_PENDING
- [ ] INTEGRATION_VALIDATED candidate

Architect makes final decision.
