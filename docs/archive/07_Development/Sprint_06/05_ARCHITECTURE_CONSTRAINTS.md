---
title: Sprint 06 Architecture Constraints
version: 1.0.0
status: Approved
---

# Domain

May contain pure:
- normalized configuration values,
- validation/comparison result models.

Must not reference:
- WPF,
- Ping,
- System.Management,
- Windows identity APIs.

# Application

Owns:
- validation/orchestration,
- comparison,
- conflict-probe abstraction,
- preflight service contracts/results.

No WPF.

# Infrastructure

Owns:
- real ICMP/network probe implementation,
- fresh adapter reads through existing reader.

No mutation implementation in this sprint.

# App

May consume the validation/preflight contracts if needed for tests or future
wiring, but Sprint 06 should not expose a working Apply button.

Do not put parsing/validation rules directly in XAML code-behind.

# Async

Network probes and fresh reads must be async/non-blocking from the UI's point of
view.

Do not scatter `Task.Run` in ViewModels.

# Dependencies

Prefer existing .NET APIs.

Do not add a package for IPv4 parsing, ping or subnet arithmetic unless there is
a concrete necessity.

# Analyzer policy

Fix analyzer findings instead of broadly disabling rules.

Existing scoped CA1707 test naming exception remains acceptable.
