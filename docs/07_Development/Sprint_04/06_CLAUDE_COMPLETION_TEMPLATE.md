# Sprint 04 Completion Report

Revision 2 — updated after `SPRINT_04_ARCHITECT_REVIEW.md`
(status: *Changes Requested Before Final Approval*). All four required
corrections are implemented; see "Architect review corrections" below.

## Build

- Command/environment: `dotnet build IPMan.sln` and `dotnet build IPMan.sln -c Release`
  on Windows 11 x64, .NET SDK 8.0.423 (pinned by `global.json`).
- Result: succeeded in Debug and Release.
- Warning count: 0
- Error count: 0

No analyzer rule was disabled to reach this result, with one scoped exception
documented under "Analyzer policy" below.

## Tests

- Tests run: 94 (`dotnet test IPMan.sln`, Debug and Release, executed 4 times)
- Passed: 94
- Failed: 0
- Skipped: 0

All tests are read-only. No test reads, writes or mutates the local machine's
network configuration; every network-facing dependency is replaced by a fake.

## Files changed

### Added — `IPMan.Domain`

| File | Purpose |
|---|---|
| `Networking/Ipv4AddressAssignment.cs` | One IPv4 address with its mask, as Windows reports it. |
| `Networking/Ipv4AddressCollection.cs` | Immutable, order-preserving IPv4 address list with sequence equality. |

### Added — `IPMan.Application`

| File | Purpose |
|---|---|
| `Common/IDelayProvider.cs` | Abstraction over waiting so debounce logic is deterministically testable. |
| `Networking/NetworkChangeReason.cs` | Technical (non user-facing) refresh reason identifiers. |
| `Networking/IAdapterRefreshCoordinator.cs` | Contract for coalesced, non-overlapping discovery. |
| `Networking/AdapterRefreshCoordinator.cs` | Debounces network events into a single discovery pass, runs the reconciliation fallback and publishes added/removed/changed. |
| `Networking/AdapterRefreshCoordinatorOptions.cs` | Debounce window (300 ms) and reconciliation interval (15 s). |
| `Networking/AdapterRefreshedEventArgs.cs` | Published adapter set plus difference against the previous pass. |
| `Networking/AdapterRefreshFailedEventArgs.cs` | Reports a failed pass without stopping observation. |

### Added — `IPMan.Infrastructure`

| File | Purpose |
|---|---|
| `Common/SystemDelayProvider.cs` | `Task.Delay` implementation of `IDelayProvider`. |
| `Networking/AdapterReadModel.cs` | Raw, unmapped adapter data as Windows reports it. |
| `Networking/IAdapterProbe.cs` | Seam over the OS read so mapping is testable. |
| `Networking/SystemNetworkInterfaceProbe.cs` | `NetworkInterface`-based read; per-adapter failures are skipped and logged. |
| `Networking/NetworkAdapterMapper.cs` | Pure mapping to `NetworkAdapterSnapshot`; never fabricates values. |
| `Networking/AdapterDiscoveryFilter.cs` | Explicit visibility rule: software loopback hidden, everything else discoverable. |
| `Networking/NetworkAdapterReader.cs` | `INetworkAdapterReader`; moves the synchronous OS read off the calling thread. |
| `Networking/INetworkChangeEventSource.cs` | Internal seam over the static `NetworkChange` events. |
| `Networking/SystemNetworkChangeEventSource.cs` | Subscribes to `NetworkAddressChanged` / `NetworkAvailabilityChanged` only while a subscriber exists. |
| `Networking/NetworkChangeMonitor.cs` | `INetworkChangeMonitor`; idempotent start/stop/dispose. |

### Added — `IPMan.App`

| File | Purpose |
|---|---|
| `Presentation/IUiDispatcher.cs` | UI synchronization boundary contract. |
| `Presentation/WpfUiDispatcher.cs` | WPF `Dispatcher` implementation. |
| `Resources/Strings.resx` | Turkish UI strings for the diagnostic view. |
| `Resources/Strings.cs` | Typed access to the localized strings. |
| `ViewModels/AdapterDiagnosticItem.cs` | Read-only presentation projection of a snapshot. |

### Added — tests

`tests/IPMan.Tests/IPMan.Tests.csproj` plus:

| File | Purpose |
|---|---|
| `TestData.cs` | Adapter/snapshot builders. |
| `Fakes/FakeAdapterProbe.cs` | OS read fake. |
| `Fakes/FakeNetworkAdapterReader.cs` | Reader fake with queued results and failure injection. |
| `Fakes/FakeNetworkChangeMonitor.cs` | Monitor fake that raises change events on demand. |
| `Fakes/FakeNetworkChangeEventSource.cs` | Counts subscribe/unsubscribe against the internal seam. |
| `Fakes/FakeDelayProvider.cs` | Per-duration waits that complete only when the test releases them, so debounce and reconciliation are driven independently. |
| `Networking/NetworkAdapterMapperTests.cs` | Mapping behaviour, including preservation of all IPv4 addresses. |
| `Networking/AdapterDiscoveryFilterTests.cs` | Adapter visibility policy. |
| `Networking/NetworkAdapterReaderTests.cs` | Discovery, filtering, per-adapter resilience, cancellation. |
| `Networking/NetworkChangeMonitorTests.cs` | Subscription lifecycle. |
| `Networking/AdapterRefreshCoordinatorTests.cs` | Coalescing, diffing, reconciliation fallback, failure handling, stop/dispose. |
| `Networking/Ipv4AddressCollectionTests.cs` | Address-collection equality and its effect on snapshot change detection. |

### Modified

| File | Change |
|---|---|
| `src/IPMan.Domain/Networking/NetworkAdapterSnapshot.cs` | Carries all IPv4 addresses plus `AdditionalIpv4Addresses` / `HasAdditionalIpv4Addresses`. |
| `src/IPMan.App/App.xaml.cs` | Registers the discovery/observation services and initializes the shell ViewModel. |
| `src/IPMan.App/ViewModels/MainWindowViewModel.cs` | Observes the coordinator and projects adapters onto the UI thread. |
| `src/IPMan.App/ViewModels/AdapterDiagnosticItem.cs` | Shows additional IPv4 addresses in the diagnostic view. |
| `src/IPMan.App/Views/MainWindow.xaml` | Temporary read-only adapter grid and status line. |
| `src/IPMan.App/Resources/Strings.resx`, `Strings.cs` | Added the additional-addresses column caption. |
| `src/IPMan.App/IPMan.App.csproj` | Added `Microsoft.Extensions.Logging.Abstractions` reference. |
| `src/IPMan.Infrastructure/IPMan.Infrastructure.csproj` | `InternalsVisibleTo` for the monitor lifecycle tests. |
| `Directory.Packages.props` | Test-only package versions. |
| `IPMan.sln` | Added the test project. |
| `README.md`, `CHANGELOG.md` | Now describe the repository's production code accurately. |

## Behavior implemented

- Adapter discovery through `System.Net.NetworkInformation.NetworkInterface`
  only. No `netsh`, PowerShell, CMD or WMI is involved.
- Identity is the Windows interface id (GUID); display name and description are
  metadata only.
- Mapped fields: id, name, description, MAC, connected state, link speed,
  configuration mode, IPv4 address, subnet mask, gateway, primary and secondary
  DNS.
- Absent values are reported as `null` (empty string for MAC), never invented:
  unreported link speed, `0.0.0.0` masks/gateways and missing DNS all map to
  absence.
- IPv4/IPv6 are kept distinct: only `InterNetwork` addresses, gateways and DNS
  servers are considered for the IPv4 fields.
- Multiple IPv4 addresses: every address is preserved in `Ipv4Addresses` in
  Windows order. The first routable address is selected as the primary
  (release-1.0 editable) address, and the rest remain observable through
  `AdditionalIpv4Addresses`. An APIPA-only adapter reports its APIPA address
  truthfully.
- Only software loopback is hidden. Tunnel, virtual and VPN adapters are
  discoverable, and display names never influence visibility.
- A single unreadable adapter is skipped (logged) and does not prevent discovery
  of the others.
- Observation uses `NetworkChange.NetworkAddressChanged` and
  `NetworkAvailabilityChanged`. Event handlers only record a reason and release a
  signal; all discovery work happens on a background worker.
- A burst of Windows events is merged into one discovery pass by a 300 ms
  debounce, and only one pass runs at a time.
- A ~15 s reconciliation fallback asks for a pass through the same coalescing
  path when no Windows event has arrived, so a missed event self-heals. It never
  reads adapters itself, so it cannot overlap an in-flight pass; it stops with
  the coordinator; and sub-second intervals are rejected by the constructor.
- Each pass publishes the full adapter set plus added/removed/changed sets,
  computed by identity and snapshot value equality.
- A failed pass raises `RefreshFailed` and observation continues.
- Temporary diagnostic UI: a read-only grid plus a status line, with all
  user-facing text from `Strings.resx` (Turkish).

Verified against this machine with a throwaway read-only console harness kept
outside the repository: 4 adapters discovered, GUID identities, connected and
disconnected adapters mapped correctly, absent gateway/DNS/link speed reported
as absent, every IPv4 address listed, reconciliation passes firing with reason
`ScheduledReconciliation` and reporting no spurious changes, and no further
passes after `StopCoordinating()`.

## Architect review corrections

| Review item | Resolution |
|---|---|
| 1 — Preserve all IPv4 addresses | `Ipv4AddressAssignment` and `Ipv4AddressCollection` were added to `IPMan.Domain` (immutable, Windows-API independent, sequence equality). `NetworkAdapterSnapshot.Ipv4Addresses` holds every IPv4 address in Windows order; `Ipv4Address`/`SubnetMask` still identify the single primary address that release 1.0 edits, and `AdditionalIpv4Addresses`/`HasAdditionalIpv4Addresses` expose the rest. Because the collection compares by sequence, an address appearing or disappearing is reported as a *changed* adapter. No mutation was implemented. |
| 2 — Adapter visibility policy | `AdapterDiscoveryFilter` now hides software loopback only. Tunnel, virtual and VPN adapters are discoverable, and no display-name heuristic exists anywhere in the code. Filter and reader tests were updated accordingly. |
| 3 — Reconciliation fallback | `AdapterRefreshCoordinator` runs a second loop that waits `ReconciliationInterval` (default 15 s) and then calls the same `RequestRefresh` path used by Windows events. It never reads adapters itself, so it cannot overlap an active pass; it is cancelled by the generation token on stop/dispose; it never touches the UI thread. Sub-second intervals throw `ArgumentOutOfRangeException`, and a non-positive interval disables the fallback. Tests drive it through `IDelayProvider`, so no test waits 15 real seconds. |
| 4 — Documentation accuracy | `README.md` was rewritten to describe the actual repository (production code, solution layout, build/test commands, implemented vs. not-implemented scope) and `CHANGELOG.md` now records the Sprint 04 implementation and these corrections. |

Nothing outside these four corrections was implemented: no WMI, no static IP or
DHCP writing, no profiles, no logging provider, no final UI, no tray, no
notifications. Existing architecture, interfaces and tests were preserved, and
no analyzer rule was suppressed to keep the build green.

## Architecture compliance

- **Domain independence**: the types added to `IPMan.Domain` are pure data with
  no WPF, `System.Management`, filesystem or `System.Net` dependency.
- **Application/Infrastructure dependency direction**: `IPMan.Application`
  references only `IPMan.Domain`; it contains contracts and orchestration and no
  `System.Management` or WPF. Infrastructure references Application and Domain.
  `IPMan.App` is the only composition root and performs no discovery itself.
- **No network mutation**: no code path added in this sprint writes IP, mask,
  gateway, DNS or DHCP state. `INetworkAdapterConfigurator` remains
  unimplemented.
- **Event-driven observation**: Windows events remain the primary trigger. The
  only timed waits are the 300 ms debounce that consolidates event bursts and the
  15 s reconciliation fallback. No sub-second polling exists, and the coordinator
  refuses to be configured with a sub-second reconciliation interval.
- **Disposal/unsubscription**: `NetworkChangeMonitor.StopMonitoring()` and
  `Dispose()` both detach from the static `NetworkChange` events, and repeated
  start/stop/dispose calls are safe (covered by tests). The coordinator's
  `Dispose()` stops observation, and the DI container disposes both singletons on
  application exit.
- **Threading**: the synchronous Windows read runs on a background thread inside
  `NetworkAdapterReader`; the ViewModel contains no `Task.Run` and marshals state
  changes through `IUiDispatcher`. `StopCoordinating()` does not block the caller.

### Analyzer policy

The repository's `latest-recommended` analyzers with `TreatWarningsAsErrors` are
fully honoured in all production projects. The only relaxation is `CA1707`
(underscores in member names) in `IPMan.Tests`, because
`04_TEST_REQUIREMENTS.md` mandates `Method_WhenCondition_ExpectedResult` test
names. It is documented inline in the test `.csproj`.

## New dependencies

Test-only, introduced because no test project existed and CLAUDE.md §9 plus
AC-S04-012 require automated tests:

- `xunit` 2.9.2 — test framework.
- `xunit.runner.visualstudio` 2.8.2 — test discovery/execution.
- `Microsoft.NET.Test.Sdk` 17.11.1 — required host for `dotnet test`.

No new package was introduced for production code.
`Microsoft.Extensions.Logging.Abstractions` was referenced by `IPMan.App` and
`IPMan.Tests`, but it was already an approved package in
`Directory.Packages.props`. No UI framework or theme package was added.

## Known limitations

1. The UI still edits a single primary IPv4 address, as release 1.0 intends. The
   additional addresses are read, preserved and displayed in the diagnostic view,
   but no UI exists for managing them — that is future-sprint work and depends on
   the mutation design.
2. Primary-address selection is deterministic (first routable IPv4, otherwise the
   first IPv4 reported). If Windows ever reports several routable addresses, the
   first one wins; the others remain visible as additional addresses.
3. DNS is filtered to IPv4 servers; IPv6 DNS servers are not surfaced (approved
   by the review).
4. Configuration mode comes solely from `IPv4InterfaceProperties.IsDhcpEnabled`.
   When Windows does not expose it, the mode is `Unknown` rather than guessed.
5. `SystemNetworkInterfaceProbe` and `SystemNetworkChangeEventSource` depend on a
   live Windows network stack and are covered by manual verification (see above)
   rather than unit tests, as permitted by AC-S04-012. All deterministic logic
   sits in the mapper, filter, reader, monitor and coordinator, which are tested.
6. Adapter *configurability* is not modelled yet. Every non-loopback adapter is
   discoverable, but the snapshot carries no "this adapter cannot be configured"
   flag; the review's expectation that the UI communicates that explicitly needs
   such a flag in a later sprint.
7. `StopCoordinating()` deliberately does not wait for an in-flight pass; the
   pass is cancelled and its result discarded instead, so the UI thread is never
   blocked at shutdown.
8. No logging provider is registered — infrastructure logs go to `NullLogger`
   until the logging sprint.
9. `MainWindowViewModel` has no unit tests: it depends on WPF types through the
   App project, and this sprint's UI is explicitly temporary.

## Questions requiring architect/product review

1. Limitation 6 above: should a later sprint add an explicit "configurable /
   not configurable" capability flag to `NetworkAdapterSnapshot` so the UI can
   state why an adapter cannot be edited? The review asks for that behaviour, but
   defining it is mutation-design work and was left out of Sprint 04.
2. Confirm that `AdditionalIpv4Addresses` (address + mask, no origin/lifetime
   data) is enough context for the future mutation safety checks, or whether
   discovery should also preserve DHCP-vs-manual origin per address.

## Diff for architect review

`git diff --stat` could not be produced: `C:\Development\IPMan` is not a git
working tree (`git rev-parse` reports "not a repository"), so there is no baseline
to diff against. The "Files changed" section above lists every added and modified
file instead. A `git init` plus baseline commit can be made on request, after
which real diffs will be available for the next review round.

## Do not continue

Sprint 05 has not been started. Awaiting architect review of these corrections.
