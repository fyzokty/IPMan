---
title: ADR-016 - Recovery Snapshot Restore and Retention
status: Accepted
date: 2026-08-20
---

# Context

Snapshots have been written before every mutation since Sprint 07, but nothing
ever read them back: `IRecoverySnapshotRepository` had only `SaveAsync`. The
product requires a one-click `Son Yapılandırmayı Geri Yükle`
(`docs/01_Product_Requirements.md` section 11, AC-008, UC-05). Two facts in the
existing design shape the answer: the file name is `recovery-{guid}.json` and
carries neither adapter nor timestamp, and nothing ever deletes a snapshot.

# Decision

**Restore is a separate service, not a method on the apply service.**
`StaticIpv4ApplyService` is built around a desired `StaticIpv4Configuration`,
text validation and a duplicate-IP probe. A restore has none of those: its input
is a recorded past state, already validated when it was captured. Folding it in
would make preflight and the mutation plan meaningless for half the callers.

**Locating a snapshot means reading them all.** Since the file name carries no
adapter identity, the repository enumerates `recovery-*.json`, deserializes each,
filters by `AdapterId` and picks the largest `CapturedAtUtc`. A malformed file is
skipped and left on disk; it must not fail the load, matching how profiles handle
corruption (ADR-015).

**Snapshots are pruned per adapter.** Keep the most recent N (default 10) for each
adapter, pruning after a successful save. Without this the directory grows without
bound and the load above gets slower with every apply. Pruning is best-effort: a
failed delete never fails the save that triggered it, and the newest snapshot is
never a pruning candidate.

**Identity is re-read immediately before the restore mutation.** EC-003 forbids
silently redirecting a change to a different adapter, and a snapshot can be
minutes or weeks old. The restore re-reads through `INetworkAdapterRecoveryReader`,
which resolves by `SettingID` and reports ambiguity as its own status, and aborts
unless the identity still matches the snapshot.

**A failed restore preserves the snapshot.** EC-002 requires rollback data to
survive a failed operation, so nothing is deleted on the failure path and the same
snapshot stays available for a second attempt.

**No route cleanup during the DHCP transition.** The workflow document says "route
cleanup only if required" without defining the condition. DHCP negotiates its own
default route, and `WindowsIpv4DefaultRouteManager.Clear` removes persistent routes
that the user may own for reasons unrelated to this adapter's address. Not clearing
is the reversible choice; if a stale static route is observed surviving a DHCP
transition on real hardware, revisit this with evidence.

# Consequences

- Restoring a snapshot whose `Mode` is `Dhcp` depends on the DHCP path, which is
  why both land in the same task.
- Snapshots older than the retention window cannot be restored. Only the most
  recent per adapter is reachable through the UI anyway.
- `EnableDHCP` return codes are mapped with the ordinary rule — `0` succeeded,
  `1` succeeded pending restart, anything else failed. The special prior-state
  code handling that `EnableStatic` needs is deliberately not copied: no evidence
  exists that `EnableDHCP` shares it, and inventing it would hide real failures.
- `RecoverySnapshotState` keeps its single `Captured` value. Recording "restored"
  would mean rewriting snapshot files after a mutation, which no requirement asks
  for and which would put a second writer on the recovery path.
