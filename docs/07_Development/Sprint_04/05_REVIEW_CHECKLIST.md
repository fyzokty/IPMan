---
title: Sprint 04 Architecture Review Checklist
version: 1.0.0
status: Approved
---

# Review checklist for Architect/Tech Lead

Use this after Claude Code finishes implementation.

## Architecture

- [ ] Domain remains Windows/WPF independent.
- [ ] Application does not reference Infrastructure.
- [ ] Infrastructure owns Windows API implementation.
- [ ] App does not perform adapter discovery directly.
- [ ] Existing interfaces were reused rather than bypassed.

## Adapter identity/state

- [ ] Display name is not used as identity.
- [ ] Missing gateway/DNS/IP is handled safely.
- [ ] Multiple IPv4 addresses are not destructively simplified.
- [ ] IPv6 is not accidentally treated as IPv4.
- [ ] Loopback/tunnel/virtual adapter behavior is explicit rather than accidental.

## Monitoring

- [ ] `StartMonitoring()` does not double-subscribe.
- [ ] `StopMonitoring()` unsubscribes.
- [ ] `Dispose()` cleans up.
- [ ] Static event subscriptions cannot leak the service.
- [ ] Event handlers are lightweight.
- [ ] No sub-second polling loop exists.

## Threading

- [ ] UI thread is not blocked.
- [ ] No uncontrolled `Task.Run` usage in ViewModels.
- [ ] Observable UI state changes occur on a valid UI synchronization boundary.

## Quality

- [ ] Zero warnings.
- [ ] Zero errors.
- [ ] Nullable warnings were fixed, not suppressed.
- [ ] Analyzer rules were not broadly disabled.
- [ ] No unnecessary NuGet dependencies.
- [ ] Tests are read-only.

## Scope

- [ ] No IP/DNS/gateway/DHCP mutation implemented.
- [ ] No profile system implemented early.
- [ ] No final UI implemented early.
