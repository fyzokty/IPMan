# TASK-002: DHCP apply path and recovery snapshot restore

- **Owner agent:** codex-coder
- **Status:** done
- **Depends on:** TASK-001

## Goal
Close the two engine gaps that block the apply working area described in
`docs/01_Product_Requirements.md` section 7. Today `INetworkAdapterConfigurator`
can only apply static IPv4, and `IRecoverySnapshotRepository` can only write
snapshots. After this task IPMan can switch an adapter to DHCP with automatic DNS
(FR-DHCP-001..003) and restore the last captured configuration for an adapter
(AC-008, UC-05). No user interface is added; the UI task consumes these services.

## Scope
Paths this task may modify:
- `src/IPMan.Domain/Networking/**` (new DHCP plan and restore types only)
- `src/IPMan.Application/Networking/**` (new DHCP and restore services only)
- `src/IPMan.Infrastructure/Networking/**` (configurator DHCP method, repository load, retention)
- `src/IPMan.App/App.xaml.cs` (DI registration only)
- `tests/IPMan.Tests/Networking/**`, `tests/IPMan.Tests/Fakes/**`

Paths this task must NOT touch:
- everything else, in particular `docs/contracts/**`, `src/IPMan.App/ViewModels/**`,
  `src/IPMan.App/Views/**`, `src/IPMan.App/Resources/**`, `src/IPMan.*/Profiles/**`,
  `src/IPMan.*/Settings/**`, any `*.csproj`

## Acceptance criteria
- [ ] `INetworkAdapterConfigurator` gains a DHCP method; WMI `EnableDHCP` is invoked
      through the existing `IWmiNetworkAdapterSession` seam
- [ ] The DHCP action also returns DNS to automatic (FR-DHCP-002), reusing the
      existing `SetDNSServerSearchOrder(null)` path rather than a second mechanism
- [ ] DHCP verification succeeds on mode alone; a missing lease is reported as a
      truthful transitional state, never as failure and never as an invented
      address (EC-022)
- [ ] An adapter already on DHCP produces no Windows write (no-change), matching
      BR-008 semantics in the static path
- [ ] A recovery snapshot is captured before the DHCP mutation (BR-009)
- [ ] `IRecoverySnapshotRepository` can load the most recent snapshot for one
      adapter; a malformed snapshot file is skipped, preserved on disk, and does
      not fail the load
- [ ] Restore re-reads adapter identity immediately before mutating and never
      redirects to a different adapter (EC-003)
- [ ] Restoring a snapshot whose `Mode` is `Dhcp` uses the new DHCP path;
      restoring a `Static` snapshot reapplies address, mask, gateway metric and
      DNS servers
- [ ] Restore failure preserves the snapshot file (EC-002)
- [ ] Snapshot retention prunes old files per adapter; pruning never deletes the
      snapshot being restored and never fails the save that triggered it
- [ ] `StaticIpv4ApplyService` behaviour is unchanged and
      `StaticIpv4ApplyServiceTests.cs` passes without a single line changed
- [ ] No new NuGet package; no `Process.Start`; no `netsh`

## Verification
```
dotnet build
dotnet test
```
All mutation paths are covered by unit tests through the existing fake WMI session
seam. **No destructive network test runs on the development machine** — real-adapter
proof requires an isolated VM with the `IPMAN_*` opt-in, which is the user's call.

## Out of scope
No UI, no `Uygula` button, no rollback button, no profiles panel, no toast
notifications. No IPv6. No renew/release. No route cleanup during the DHCP
transition (see ADR-016 for why). `RecoverySnapshotState` stays single-valued;
restore does not write state back into snapshot files.

## Notes
Read first: `docs/ADR/ADR-008-WMI-Network-Mutation.md` (already lists `EnableDHCP`
as expected), `docs/ADR/ADR-016-Recovery-Restore-And-Retention.md`,
`docs/02_Architecture/04_Configuration_Apply_Workflow.md` DHCP pipeline section,
`docs/01_Product_Requirements.md` sections 10 and 11,
`docs/08_Edge_Cases.md` EC-002, EC-003, EC-022.

Delivered in two sequential codex-coder runs:
A = DHCP path. B = restore path and retention.
