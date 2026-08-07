# Sprint 07 Completion Report

## Build

- Debug: `dotnet build IPMan.sln` — succeeded
- Release: `dotnet build IPMan.sln -c Release` — succeeded
- Warnings: 0
- Errors: 0

## Tests

- Total: 297
- Passed: 297
- Failed: 0
- Skipped: 0

68 deterministic Sprint 07 test cases were added. All 229 existing tests remain
passing. The default suite never invokes a real WMI session or mutates an
adapter.

## Git

`git status --short` captured before the architect-review correction (the final
review-fix handoff contains the current status):

```text
 M CHANGELOG.md
 M PACKAGE_MANIFEST.md
 M README.md
 M src/IPMan.App/App.xaml.cs
 M src/IPMan.Application/Networking/INetworkAdapterConfigurator.cs
 M src/IPMan.Domain/Networking/NetworkAdapterSnapshot.cs
 M src/IPMan.Domain/Networking/NetworkApplyResult.cs
 M src/IPMan.Infrastructure/Networking/NetworkAdapterMapper.cs
 M tests/IPMan.Tests/Networking/NetworkAdapterMapperTests.cs
 M tests/IPMan.Tests/TestData.cs
?? docs/07_Development/Sprint_07/
?? src/IPMan.Application/Networking/IRollbackSnapshotRepository.cs
?? src/IPMan.Application/Networking/IStaticIpv4ApplyService.cs
?? src/IPMan.Application/Networking/RollbackCaptureFailure.cs
?? src/IPMan.Application/Networking/RollbackCaptureResult.cs
?? src/IPMan.Application/Networking/StaticIpv4ApplyOptions.cs
?? src/IPMan.Application/Networking/StaticIpv4ApplyRequest.cs
?? src/IPMan.Application/Networking/StaticIpv4ApplyResult.cs
?? src/IPMan.Application/Networking/StaticIpv4ApplyService.cs
?? src/IPMan.Application/Networking/StaticIpv4ApplyStatus.cs
?? src/IPMan.Application/Networking/StaticIpv4SafetyBlock.cs
?? src/IPMan.Domain/Networking/GatewayMutationMode.cs
?? src/IPMan.Domain/Networking/Ipv4AddressValueCollection.cs
?? src/IPMan.Domain/Networking/NetworkMutationFailureKind.cs
?? src/IPMan.Domain/Networking/NetworkMutationStepResult.cs
?? src/IPMan.Domain/Networking/NetworkMutationStepStatus.cs
?? src/IPMan.Domain/Networking/NetworkRollbackSnapshot.cs
?? src/IPMan.Domain/Networking/RollbackSnapshotReference.cs
?? src/IPMan.Domain/Networking/RollbackSnapshotState.cs
?? src/IPMan.Domain/Networking/StaticIpv4MutationPlan.cs
?? src/IPMan.Infrastructure/Networking/IWmiNetworkAdapterSession.cs
?? src/IPMan.Infrastructure/Networking/IWmiNetworkAdapterSessionFactory.cs
?? src/IPMan.Infrastructure/Networking/JsonRollbackSnapshotRepository.cs
?? src/IPMan.Infrastructure/Networking/RollbackSnapshotRepositoryOptions.cs
?? src/IPMan.Infrastructure/Networking/SystemWmiNetworkAdapterSessionFactory.cs
?? src/IPMan.Infrastructure/Networking/WmiAdapterResolution.cs
?? src/IPMan.Infrastructure/Networking/WmiAdapterResolutionStatus.cs
?? src/IPMan.Infrastructure/Networking/WmiNetworkAdapterConfigurator.cs
?? tests/IPMan.Tests/Fakes/FakeNetworkAdapterConfigurator.cs
?? tests/IPMan.Tests/Fakes/FakeNetworkConfigurationPreflightService.cs
?? tests/IPMan.Tests/Fakes/FakeRollbackSnapshotRepository.cs
?? tests/IPMan.Tests/Fakes/ImmediateDelayProvider.cs
?? tests/IPMan.Tests/Networking/Ipv4AddressValueCollectionTests.cs
?? tests/IPMan.Tests/Networking/JsonRollbackSnapshotRepositoryTests.cs
?? tests/IPMan.Tests/Networking/StaticIpv4ApplyServiceTests.cs
?? tests/IPMan.Tests/Networking/WmiNetworkAdapterConfiguratorTests.cs
```

`git diff --stat` (untracked files are not included by Git):

```text
 CHANGELOG.md                                       | 27 +++++-----
 PACKAGE_MANIFEST.md                                | 24 +++++----
 README.md                                          | 22 +++-----
 src/IPMan.App/App.xaml.cs                          | 12 +++++
 .../Networking/INetworkAdapterConfigurator.cs      |  6 +--
 .../Networking/NetworkAdapterSnapshot.cs           |  6 ++-
 src/IPMan.Domain/Networking/NetworkApplyResult.cs  | 27 ++++++----
 .../Networking/NetworkAdapterMapper.cs             | 63 +++++++++++-----------
 .../Networking/NetworkAdapterMapperTests.cs        | 19 +++++++
 tests/IPMan.Tests/TestData.cs                      | 11 +++-
 10 files changed, 127 insertions(+), 90 deletions(-)
```

The `CHANGELOG.md`, `PACKAGE_MANIFEST.md` and `README.md` changes predated this
sprint implementation and were preserved.

## Files changed

- Domain: ordered IPv4 gateway/DNS collection, complete rollback document,
  typed mutation plan, per-step results and failure categories.
- Application: rollback repository contract and full static-apply service with
  typed request/result, topology gates, warning confirmations, global mutation
  serialization and bounded verification.
- Infrastructure: complete state mapping, atomic JSON rollback repository,
  exact-identity WMI session factory and static configurator.
- App: dependency-injection registration only. No Apply or DHCP control is
  wired.
- Tests: full-state mapping, rollback persistence, WMI return/step mapping and
  end-to-end orchestration with fakes.

## Full state model

`NetworkAdapterSnapshot` now preserves every IPv4 default gateway and DNS server
in Windows-reported order while retaining the existing convenience properties.
Static apply blocks more than one IPv4 assignment, more than one gateway or more
than two DNS servers before rollback or mutation. Zero gateway and zero DNS are
supported.

## Rollback

Snapshots are written under `%LocalAppData%\IPMan\Backup` with schema version 2,
correlation ID, UTC timestamp, stable adapter identity/metadata, mode and all
IPv4/mask values, gateway/metric pairs, effective DNS values, configured DNS
source and adapter-level manual DNS values. JSON is serialized to a
same-directory temp file, flushed/closed and moved to a unique final filename.
Typed I/O/access failure aborts apply before WMI. Successful snapshots are never
deleted after verification.

## WMI mutation

WMI resolution queries `Win32_NetworkAdapterConfiguration` by the exact escaped
`SettingID` and rejects zero/multiple exact matches. Mutation order is
`EnableStatic`, `SetGateways`, `SetDNSServerSearchOrder`.

For gateway-empty with an existing gateway, `SetGateways` receives the same host
address passed to `EnableStatic`, which Microsoft explicitly documents as the
gateway-clear sentinel. If no gateway already exists, the unnecessary gateway
call is skipped. For DNS-empty, `SetDNSServerSearchOrder` is invoked without any
input parameters, which Microsoft documents as returning from static DNS to
DHCP/automatic source semantics.

Return 0 maps to success and 1 to success/restart-required. EnableStatic code 81
maps to a retained-code provisional success only when the immediately preceding
exact-identity recovery read proves the adapter was already static. Code 81 for
DHCP/unknown prior mode or either later WMI method remains a failure. Management
failures are typed; unexpected programming defects propagate. IP success followed
by gateway/DNS failure is represented as partial failure and triggers a fresh
read, never product-level success.

Engineering references:

- https://learn.microsoft.com/windows/win32/cimwin32prov/enablestatic-method-in-class-win32-networkadapterconfiguration
- https://learn.microsoft.com/windows/win32/cimwin32prov/setgateways-method-in-class-win32-networkadapterconfiguration
- https://learn.microsoft.com/windows/win32/cimwin32prov/setdnsserversearchorder-method-in-class-win32-networkadapterconfiguration
- https://learn.microsoft.com/windows/win32/cimwin32prov/win32-networkadapterconfiguration
- https://learn.microsoft.com/windows/win32/api/netioapi/nf-netioapi-getinterfacednssettings
- https://learn.microsoft.com/windows/win32/api/netioapi/ns-netioapi-dns_interface_settings

## Apply workflow

Sprint 06 preflight runs first. Blocking/error/no-change statuses stop without
rollback or WMI. Potential conflict and indeterminate probe results each require
explicit request state before continuing. A second exact-SettingID WMI recovery
read then re-checks mode, comparison and all topology gates; its state alone
feeds rollback and the mutation plan. Drift to NoChange or an unsafe topology
stops before persistence or mutation. A singleton injectable Application-layer
`INetworkMutationCoordinator` serializes the full transaction and is reusable by
future DHCP/restore services. After mutation starts, cancellation does not claim
rollback; the service completes a fresh read. Verification performs one immediate
read plus bounded delayed retries. Only normalized comparer equivalence plus exact
single-address/gateway/DNS full-state equivalence returns `VerifiedSuccess`.

## Tests added

- Zero/one/multiple ordered gateways and zero/one/two/three DNS servers.
- Every topology safety gate and stable adapter identity forwarding.
- Atomic JSON content, duplicate-final protection and persistence failure.
- WMI method order/parameters, 0/1/error code mapping, restart, management
  failure, partial failure, DNS-empty no-input call and documented gateway-clear
  sentinel.
- EnableStatic 81 with static versus DHCP/unknown prior mode, plus proof that 81
  is not globally successful for gateway or DNS methods.
- Preflight-to-mutation drift: disappearance, NoChange, second IPv4, multiple
  gateways, third DNS, mode changes and rollback sourcing from the second read.
- Shared coordinator serialization across separate static-apply service instances.
- DNS automatic/manual/unknown semantics, configured manual DNS capture and
  gateway-metric pairing/incomplete recovery blocking.
- Validation/no-change/preflight stops, conflict/probe confirmations, rollback
  ordering/failure, immediate/delayed/failed verification, disappearance/read
  failure, cancellation before/after mutation and concurrent serialization.

## New dependencies

`None`

## Manual/integration verification

No real Windows adapter was mutated. The concrete WMI implementation compiled
on Windows and its return-code/step logic was exercised through the internal WMI
session seam only. Real mutation must be validated later on a disposable VM or
explicitly designated isolated adapter with recovery access.

## Known limitations / blockers

The WMI behavior and mandatory IPv6 non-interference gate have not yet been
exercised against a disposable/isolated real adapter. `GetInterfaceDnsSettings`
is documented for Windows 10 build 19041 and later; an older Windows 10 build,
profile/policy DNS source, failed native read or incomplete static gateway metric
is conservatively `Unknown`/not restore-capable and blocks mutation. No shell
fallback exists.

## Questions for architect

None for the implemented Sprint 07 scope. Gateway-clear and DNS-empty behavior
were resolved from the Microsoft Learn method documentation cited above.

## Do not continue

Sprint 08 was not started. No commit was created; changes remain for architect
review.
