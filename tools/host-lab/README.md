# HOST isolated adapter lab runner

This developer-only runner discovers and validates the HOST-side Hyper-V
`vEthernet (IPMan-Test-Switch)` adapter directly from the Windows networking
stack. Hyper-V PowerShell cmdlets and Hyper-V Administrators membership are not
discovery prerequisites.

Windows PowerShell 5.1 is the compatibility baseline. Run from the repository
root. Controlled mutation normally requires an elevated local console, but the
runner never attempts to elevate or change authorization.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
  tools/host-lab/Invoke-IPManHostLab.ps1 -Action Preflight

powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
  tools/host-lab/Invoke-IPManHostLab.ps1 -Action DiscoverTarget
```

After discovery, use the reported exact InterfaceGuid as the authority:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
  tools/host-lab/Invoke-IPManHostLab.ps1 -Action DnsTruthDiagnostic `
  -ExpectedAdapterGuid '00000000-0000-0000-0000-000000000000'
```

The only mutation action is `ControlledValidation`. It refuses to run without
the exact GUID returned by direct discovery and a manually elevated Windows
PowerShell process. A non-elevated run returns
`ELEVATION_REQUIRED_FOR_MUTATION` before any mutation scenario is launched. It
invokes the existing production integration harness for four fixed transitions:
manual DNS, automatic DNS rollback, temporary IPv4 change, and IPv4 rollback.

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
  tools/host-lab/Invoke-IPManHostLab.ps1 -Action ControlledValidation `
  -ExpectedAdapterGuid '00000000-0000-0000-0000-000000000000'
```

Before mutation the runner persists a complete snapshot. Each process attempt
is followed by a fresh Windows adapter/IP/route/DNS read. `mutationAttempted`
means that the destructive scenario child process actually started;
`mutationPerformed` means that a fresh snapshot observed a real target-state
transition. If a failed apply leaves the target strictly equal to baseline, the
runner records the rollback step as skipped and does not issue a redundant
rollback mutation.

Live DNS snapshots normalize the Windows `AddressFamily` representation (`2/23`
or `IPv4/IPv6`) before classifying server lists. After the fresh snapshot and
mandatory rollback checks, a specific process/harness failure takes precedence
over `MutationVerificationFailed`; missing expected OS transition is reported
as verification failure only when process execution proof itself passed.

Target equality treats only the exact Windows unconfigured IPv6 DNS sentinel
trio (`fec0:0:0:ffff::1`, `::2`, and `::3`, with normal zone/scope suffixes) as
equivalent to an empty IPv6 DNS list. Raw IPv6 DNS observations remain unchanged
in evidence. Partial trios, additional or real DNS values, IPv6 addresses, and
IPv6 routes remain strict and fail closed. This semantic rule is shared by
mutation verification, rollback equality, and final equality.

Substantive non-target drift—adapter identity, address, default-route
identity/next hop, or DNS—fails closed with
`NON_TARGET_STATE_CHANGED`. A non-target default-route `RouteMetric` or
`InterfaceMetric` change is retained in `non-target-drift.json` and summary
evidence, but metric-only drift does not become `ROLLBACK_FAILED`. No gateway,
DHCP, IPv6, other-adapter, VM, switch, firewall, service, group, policy, or
registry change is authorized.

`DnsTruthDiagnostic` runs only the `DnsTruthDiagnostic` category in
`IPMan.IntegrationTests`. The runner clears all `IPMAN_*` variables in the child
process, sets only the diagnostic opt-in and exact adapter GUID, and captures an
AFTER snapshot before validating process exit or output markers. Network-state
change therefore takes precedence over diagnostic failure.

Every snapshot independently re-reads the Windows adapter list, requires one
exact match for the discovered InterfaceGuid, and verifies that its
InterfaceIndex has not drifted. Snapshot address, route, and DNS state is read
only after that fresh identity check; the runner never retargets by alias.

Evidence is written beneath the ignored `artifacts/host-lab/` directory. Every
run writes `summary.json`, `transcript.txt`, `before.json`, `after.json`, and
`dns-truth-diagnostic.txt`. Controlled validation additionally writes original,
mutated, rollback, and fixed-scenario process evidence. A failed safety check is
recorded with a typed failure code and fails closed.

Run deterministic self-tests without requiring Hyper-V:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File `
  tools/host-lab/tests/Invoke-IPManHostLab.SelfTests.ps1
```
