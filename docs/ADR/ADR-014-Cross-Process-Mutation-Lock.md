---
title: ADR-014 - Two Named Locks with Different Scopes
status: Accepted
date: 2026-08-18
---

# Decision

IPMan takes two named Windows locks with deliberately different scopes:

- `Local\IPMan.SingleInstance` establishes the interactive instance, as
  [ADR-011](ADR-011-Single-Instance.md) requires.
- `Global\IPMan.NetworkMutation` serializes network mutation across every
  process on the machine. ADR-011 does not cover this lock; it is new here.

Both are `Mutex`, not `Semaphore`.

# Why the scopes differ

Activation is inherently per-session. A window in another Windows logon session
cannot be restored or focused, so a machine-wide single-instance lock could
never satisfy PR-021's "bring the existing instance to the foreground" for a
second user — it could only refuse to start. Session scope is the only scope in
which the requirement is meaningful.

Network configuration is the opposite: it is machine-wide state. Two users in
separate sessions mutating the same adapter is a real race, and NFR-REL-003 and
NFR-REL-004 both break under it — the recovery snapshot stops describing the
true prior state, and verification by re-read compares against someone else's
change.

Keeping the locks separate also avoids a deadlock of our own making. Per
`07_Application_Lifecycle.md:47-50` shutdown must not abandon a running
mutation, so a closing instance can legitimately outlive its window. If one lock
served both purposes, a second launch could not become the interactive instance
for the entire duration of the first instance's mutation-completion path.

# Why Mutex rather than Semaphore

A named `Semaphore` composes cleanly with async code, but nothing restores its
count if the holder dies. A crash during mutation would lock every later
instance out until logoff.

A named `Mutex` is released by Windows when its owner dies, and the next waiter
observes `AbandonedMutexException` while still being granted ownership. The
cost is thread affinity: mutex ownership cannot cross an async continuation, so
`GlobalNamedMutexLock` keeps the whole wait-own-release sequence on a dedicated
thread and reports completion through a `TaskCompletionSource`. The
single-instance guard needs no such machinery — it is acquired once on the UI
thread at startup and released on the same thread at exit.

# Abandoned locks

Both locks accept ownership after abandonment and continue normally. The goal
here is only to prevent a crashed instance from deadlocking the next one.

EC-019 additionally asks that a crash between snapshot and verification leave
the snapshot and unexpected-shutdown information recoverable. That surface
belongs to the crash-marker feature (PR-026), which has not been started, so an
abandoned mutation lock is currently handled but not reported to the user.

# The activation channel

The pipe carries one byte and nothing else, per ADR-011's rule that the IPC
"accepts only the minimal activation message". No payload, no command-line
forwarding — no requirement asks for either.

Its ACL is set explicitly to `BUILTIN\Administrators` with inheritance
disabled, rather than relying on the default. [ADR-013](ADR-013-Single-Process-Elevation.md)
names the hazard: IPMan runs elevated, and a high-integrity process exposing a
pipe reachable from medium integrity is the classic UAC bypass shape. Every
IPMan instance is elevated, so restricting the pipe to administrators costs
nothing.

The second process calls `AllowSetForegroundWindow` before it signals.
Without it `Activate()` silently loses to the Windows foreground lock and only
flashes the taskbar button, which would not satisfy AC-017.

Activation never touches the mutation lock. It is a pure UI-visibility
operation, so a second launch stays responsive while an apply is in flight.

# No timeout on the mutation lock

The lock waits only on the caller's `CancellationToken`, matching the previous
in-process `SemaphoreSlim` contract. A timeout would surface as an
`OperationCanceledException` that `StaticIpv4ApplyService` does not catch — its
filter is `when (cancellationToken.IsCancellationRequested)` — so the exception
would escape instead of becoming a result. Adding one would require a new
`StaticIpv4ApplyStatus` member and a decision about what the UI shows.
