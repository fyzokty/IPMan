---
title: Sprint 06 Architect Review Checklist
version: 1.0.0
status: Approved
---

# Scope

- [ ] No Windows configuration mutation.
- [ ] No Apply/DHCP UI wiring.
- [ ] No profile/settings/tray work.

# Validation

- [ ] Uses .NET IP parsing rather than regex-only validation.
- [ ] Mask contiguity handled correctly.
- [ ] Gateway optional.
- [ ] DNS optional semantics preserved.
- [ ] Secondary-only DNS handled explicitly.
- [ ] /31 and /32 behavior tested/documented.

# Comparison

- [ ] Normalization happens once in a coherent layer.
- [ ] NoChange is based on normalized values.
- [ ] Field differences are typed/structured.
- [ ] DHCP/static mode is considered.

# Safety

- [ ] Adapter matched by identity.
- [ ] Multiple IPv4 assignments are not discarded.
- [ ] Conflict probe never claims an address is definitely free.
- [ ] Probe exceptions are handled.
- [ ] Fresh read used for application preflight.

# Architecture

- [ ] Domain has no Windows/WPF dependency.
- [ ] Application owns orchestration contracts.
- [ ] Infrastructure owns real Ping implementation.
- [ ] ViewModels do not call Ping/NetworkInterface directly.
- [ ] Async work does not block UI.

# Quality

- [ ] Debug 0 warnings/errors.
- [ ] Release 0 warnings/errors.
- [ ] All tests pass.
- [ ] No broad analyzer suppression.
- [ ] No unnecessary NuGet packages.
