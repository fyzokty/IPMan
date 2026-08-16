[CmdletBinding()]
param([string]$NamePattern = '*')

Set-StrictMode -Version 2.0
$ErrorActionPreference = 'Stop'

$modulePath = Join-Path $PSScriptRoot '../IPMan.HostLab.psm1'
$script:Module = Import-Module $modulePath -Force -PassThru
$script:Passed = 0
$script:Failed = 0
$script:Failures = New-Object System.Collections.Generic.List[string]
$script:TargetGuid = '11111111-2222-3333-4444-555555555555'
$script:OtherGuid = 'AAAAAAAA-BBBB-CCCC-DDDD-EEEEEEEEEEEE'

function Invoke-Private {
    param([string]$Name, [hashtable]$Arguments = @{})
    return & $script:Module { param($FunctionName, $FunctionArguments) & $FunctionName @FunctionArguments } $Name $Arguments
}

function Assert-True {
    param([bool]$Condition, [string]$Message = 'Expected true.')
    if (-not $Condition) { throw $Message }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message = 'Values differ.')
    if ([string]$Expected -cne [string]$Actual) { throw "$Message Expected=[$Expected] Actual=[$Actual]" }
}

function Assert-FailureCode {
    param([string]$ExpectedCode, [scriptblock]$Operation)
    try {
        & $Operation
        throw "Expected failure code $ExpectedCode but operation succeeded."
    }
    catch {
        $actual = $_.Exception.Data['HostLabFailureCode']
        if ([string]$actual -cne $ExpectedCode) { throw "Expected failure code $ExpectedCode, got $actual ($($_.Exception.Message))." }
    }
}

function Invoke-SelfTest {
    param([string]$Name, [scriptblock]$Test)
    if ($Name -notlike $NamePattern) { return }
    try {
        & $Test
        $script:Passed++
        Write-Output "PASS $Name"
    }
    catch {
        $script:Failed++
        $script:Failures.Add("$Name`: $($_.Exception.Message)")
        Write-Output "FAIL $Name -- $($_.Exception.Message)"
    }
}

function New-FakeAdapter {
    param(
        [string]$Guid = $script:TargetGuid,
        [string]$Name = 'vEthernet (IPMan-Test-Switch)',
        [string]$Mac = '00-15-5D-01-02-03',
        [int]$Index = 42,
        [int]$IfType = 6,
        [bool]$Virtual = $true,
        [bool]$Hardware = $false,
        [string]$MediaType = '802.3',
        [string]$Status = 'Up',
        [string]$Description = 'Hyper-V Virtual Ethernet Adapter'
    )
    return [pscustomobject]@{
        InterfaceGuid = $Guid
        Name = $Name
        MacAddress = $Mac
        ifIndex = $Index
        ifType = $IfType
        Virtual = $Virtual
        HardwareInterface = $Hardware
        MediaType = $MediaType
        NdisPhysicalMedium = 'Unspecified'
        InterfaceDescription = $Description
        Status = $Status
    }
}

function New-FakeNonTargetAdapter {
    return New-FakeAdapter -Guid $script:OtherGuid -Name 'Wi-Fi' -Mac 'AA-BB-CC-DD-EE-FF' -Index 7 -IfType 71 -Virtual $false -Hardware $true -MediaType 'Native 802.11' -Description 'Test Wi-Fi Adapter'
}

function New-FakeDefaultRoute {
    param(
        [string]$NextHop = '192.168.1.1',
        [int]$RouteMetric = 0,
        [int]$InterfaceMetric = 40
    )
    return [pscustomobject]@{
        InterfaceIndex = 7
        DestinationPrefix = '0.0.0.0/0'
        NextHop = $NextHop
        RouteMetric = $RouteMetric
        InterfaceMetric = $InterfaceMetric
    }
}

function New-FakeData {
    param(
        [object[]]$Switches = @([pscustomobject]@{ Name = 'IPMan-Test-Switch'; SwitchType = 'Internal' }),
        [object[]]$HyperVAdapters = @([pscustomobject]@{ Name = 'IPMan-Test-Switch'; SwitchName = 'IPMan-Test-Switch'; MacAddress = '00155D010203' }),
        [object[]]$NetAdapters = @((New-FakeAdapter)),
        [object[]]$Routes = @([pscustomobject]@{ InterfaceIndex = 7; DestinationPrefix = '0.0.0.0/0'; NextHop = '192.168.1.1'; RouteMetric = 0; InterfaceMetric = 25 })
    )
    return [pscustomobject]@{ Switches = $Switches; HyperVAdapters = $HyperVAdapters; NetAdapters = $NetAdapters; Routes = $Routes }
}

function Resolve-FakeTarget {
    param($Data, [string]$ExpectedAdapterGuid)
    $arguments = @{ Data = $Data }
    if ($PSBoundParameters.ContainsKey('ExpectedAdapterGuid')) { $arguments.ExpectedAdapterGuid = $ExpectedAdapterGuid }
    return Invoke-Private -Name 'Resolve-HostLabTarget' -Arguments $arguments
}

function New-FakeSnapshotData {
    param(
        [string]$Ipv4 = '10.250.0.1',
        [int]$Prefix = 24,
        [string]$Ipv6 = 'fe80::1',
        [string[]]$Ipv4Dns = @(),
        [string[]]$Ipv6Dns = @(),
        [object[]]$Routes = @(),
        [object[]]$NetAdapters = @((New-FakeAdapter)),
        [object[]]$ExtraAddresses = @(),
        [object[]]$ExtraDns = @(),
        [object]$Ipv4DnsFamily = 'IPv4',
        [object]$Ipv6DnsFamily = 'IPv6'
    )
    $addresses = @(
        [pscustomobject]@{ InterfaceIndex = 42; AddressFamily = 'IPv4'; IPAddress = $Ipv4; PrefixLength = $Prefix },
        [pscustomobject]@{ InterfaceIndex = 42; AddressFamily = 'IPv6'; IPAddress = $Ipv6; PrefixLength = 64 }
    ) + @($ExtraAddresses)
    $dns = @(
        [pscustomobject]@{ InterfaceIndex = 42; AddressFamily = $Ipv4DnsFamily; ServerAddresses = $Ipv4Dns },
        [pscustomobject]@{ InterfaceIndex = 42; AddressFamily = $Ipv6DnsFamily; ServerAddresses = $Ipv6Dns }
    ) + @($ExtraDns)
    return [pscustomobject]@{
        NetAdapters = $NetAdapters
        IPAddresses = $addresses
        Routes = $Routes
        Dns = $dns
    }
}

function Get-FakeSnapshot {
    param($Data)
    $target = Resolve-FakeTarget -Data (New-FakeData)
    return Invoke-Private -Name 'Get-HostLabSnapshot' -Arguments @{ Target = $target; Data = $Data }
}

function New-GoodDiagnosticOutput {
    param([string]$Guid = $script:TargetGuid)
    return @"
DnsTruthDiagnostic: Enabled
ExactAdapterId: $Guid
GetInterfaceDnsSettings:
DnsClientCimIPv4:
DnsClientCimIPv6:
GetAdaptersAddressesIPv4:
GetAdaptersAddressesIPv6:
MutationPerformed: False
Passed!  - Failed: 0, Passed: 1, Skipped: 0, Total: 1
"@
}

function New-FakeOperations {
    param(
        [object[]]$Snapshots = @((New-FakeSnapshotData), (New-FakeSnapshotData)),
        $DiagnosticResult = ([pscustomobject]@{ ExitCode = 0; Output = (New-GoodDiagnosticOutput) }),
        [object[]]$ScenarioResults = @(),
        [bool]$Elevated = $true
    )

    $snapshotQueue = New-Object System.Collections.Queue
    foreach ($snapshot in $Snapshots) { $snapshotQueue.Enqueue($snapshot) }
    $scenarioQueue = New-Object System.Collections.Queue
    foreach ($scenarioResult in $ScenarioResults) { $scenarioQueue.Enqueue($scenarioResult) }
    $calls = [pscustomobject]@{
        Prerequisites = 0
        Provenance = 0
        Discovery = 0
        Snapshot = 0
        Diagnostic = 0
        Evidence = 0
        Scenario = 0
        SaveState = 0
        SaveProcess = 0
    }
    $discoveryData = New-FakeData
    $provenance = [pscustomobject]@{
        fullSha = 'e30ccd5e64d804316a03d8e65c7cb7cb8746ad40'
        shortSha = 'e30ccd5e64d8'
        trackedWorkingTreeClean = $false
        trackedStatus = @(' A tools/host-lab/IPMan.HostLab.psm1')
    }
    $operations = @{
        AssertPrerequisites = {
            param($Root)
            $calls.Prerequisites++
        }.GetNewClosure()
        GetProvenance = {
            param($Root)
            $calls.Provenance++
            return $provenance
        }.GetNewClosure()
        GetDiscoveryData = {
            param($Name)
            $calls.Discovery++
            return $discoveryData
        }.GetNewClosure()
        GetSnapshotData = {
            $calls.Snapshot++
            if ($snapshotQueue.Count -eq 0) { throw 'Unexpected extra snapshot read.' }
            $next = $snapshotQueue.Dequeue()
            if ($null -ne $next.PSObject.Properties['ReadFailure']) { throw [string]$next.ReadFailure }
            return $next
        }.GetNewClosure()
        IsElevated = { return $Elevated }.GetNewClosure()
        RunDiagnostic = {
            param($Root, $Guid)
            $calls.Diagnostic++
            return $DiagnosticResult
        }.GetNewClosure()
        RunScenario = {
            param($Root, $Guid, $Scenario, $Ipv4, $Dns, $EvidenceRoot)
            $calls.Scenario++
            if ($scenarioQueue.Count -gt 0) { return $scenarioQueue.Dequeue() }
            $output = "DESTRUCTIVE NETWORK TEST ENABLED FOR ONE EXPLICIT SCENARIO.`nExact target adapter: {$Guid}`nScenario: $Scenario`nPassed! - Failed: 0, Passed: 1, Skipped: 0, Total: 1"
            return [pscustomobject]@{ ExitCode = 0; Output = $output; Scenario = $Scenario; ProcessStarted = $true }
        }.GetNewClosure()
        SaveState = {
            param($Directory, $Name, $Value)
            $calls.SaveState++
        }.GetNewClosure()
        SaveProcess = {
            param($Directory, $Name, $Value)
            $calls.SaveProcess++
        }.GetNewClosure()
        WriteEvidence = {
            param($Directory, $Summary, $Before, $After, $Output, $Transcript)
            $calls.Evidence++
        }.GetNewClosure()
    }
    return [pscustomobject]@{ Operations = $operations; Calls = $calls }
}

function Invoke-FakeOrchestration {
    param(
        [string]$Action,
        [Parameter(Mandatory = $true)]$FakeOperations
    )
    $arguments = @{
        Action = $Action
        RepoRoot = 'C:\deterministic-host-lab-test'
        Operations = $FakeOperations.Operations
    }
    if ($Action -eq 'ControlledValidation') { $arguments.ExpectedAdapterGuid = $script:TargetGuid }
    return Invoke-Private -Name 'Invoke-HostLabOrchestration' -Arguments $arguments
}

Invoke-SelfTest '01 exact direct vEthernet adapter is eligible' {
    $target = Resolve-FakeTarget -Data (New-FakeData)
    Assert-Equal 'Pass' $target.SafetyGate
}
Invoke-SelfTest '02 direct adapter missing fails closed' {
    Assert-FailureCode 'AliasHintNotFound' { Resolve-FakeTarget -Data (New-FakeData -NetAdapters @()) }
}
Invoke-SelfTest '03 duplicate direct adapter alias fails closed' {
    $adapters = @((New-FakeAdapter), (New-FakeAdapter -Guid $script:OtherGuid -Mac '00-15-5D-04-05-06' -Index 43))
    Assert-FailureCode 'AliasHintAmbiguous' { Resolve-FakeTarget -Data (New-FakeData -NetAdapters $adapters) }
}
Invoke-SelfTest '04 target not Up fails closed' {
    Assert-FailureCode 'TargetNotUp' { Resolve-FakeTarget -Data (New-FakeData -NetAdapters @((New-FakeAdapter -Status 'Disconnected'))) }
}
Invoke-SelfTest '05 non Hyper-V virtual description fails closed' {
    Assert-FailureCode 'TargetNotHyperVVirtualAdapter' { Resolve-FakeTarget -Data (New-FakeData -NetAdapters @((New-FakeAdapter -Description 'Generic Virtual Adapter'))) }
}
Invoke-SelfTest '06 exact GUID mismatch fails closed' {
    Assert-FailureCode 'ExpectedGuidNotFound' { Resolve-FakeTarget -Data (New-FakeData) -ExpectedAdapterGuid $script:OtherGuid }
}
Invoke-SelfTest '07 exact GUID zero match fails closed' {
    Assert-FailureCode 'ExpectedGuidNotFound' { Resolve-FakeTarget -Data (New-FakeData -NetAdapters @()) -ExpectedAdapterGuid $script:TargetGuid }
}
Invoke-SelfTest '08 exact GUID duplicate source fails closed' {
    $adapters = @((New-FakeAdapter), (New-FakeAdapter -Name 'duplicate' -Mac '00-15-5D-09-09-09' -Index 43))
    Assert-FailureCode 'ExpectedGuidAmbiguous' { Resolve-FakeTarget -Data (New-FakeData -NetAdapters $adapters) -ExpectedAdapterGuid $script:TargetGuid }
}
Invoke-SelfTest '09 IPv4 default route ownership fails closed' {
    $route = [pscustomobject]@{ InterfaceIndex = 42; DestinationPrefix = '0.0.0.0/0'; NextHop = '10.250.0.254'; RouteMetric = 0; InterfaceMetric = 25 }
    Assert-FailureCode 'TargetOwnsIpv4DefaultRoute' { Resolve-FakeTarget -Data (New-FakeData -Routes @($route)) }
}
Invoke-SelfTest '10 IPv6 default route ownership fails closed' {
    $route = [pscustomobject]@{ InterfaceIndex = 42; DestinationPrefix = '::/0'; NextHop = 'fe80::2'; RouteMetric = 0; InterfaceMetric = 25 }
    Assert-FailureCode 'TargetOwnsIpv6DefaultRoute' { Resolve-FakeTarget -Data (New-FakeData -Routes @($route)) }
}
Invoke-SelfTest '11 Wi-Fi target fails closed' {
    Assert-FailureCode 'TargetIsWifi' { Resolve-FakeTarget -Data (New-FakeData -NetAdapters @((New-FakeAdapter -IfType 71))) }
}
Invoke-SelfTest '12 physical target fails closed' {
    Assert-FailureCode 'TargetIsPhysical' { Resolve-FakeTarget -Data (New-FakeData -NetAdapters @((New-FakeAdapter -Virtual $false -Hardware $true))) }
}
Invoke-SelfTest '13 exact Hyper-V host vEthernet passes' {
    $target = Resolve-FakeTarget -Data (New-FakeData) -ExpectedAdapterGuid $script:TargetGuid
    Assert-Equal $script:TargetGuid $target.InterfaceGuid
    Assert-Equal 'DeferredCrossValidation' $target.SwitchType
}
Invoke-SelfTest '14 approved IPv4 baseline passes' {
    Assert-True (Get-FakeSnapshot -Data (New-FakeSnapshotData)).baselineMatch
}
Invoke-SelfTest '15 wrong IPv4 is BaselineMismatch' {
    Assert-True (-not (Get-FakeSnapshot -Data (New-FakeSnapshotData -Ipv4 '10.250.0.2')).baselineMatch)
}
Invoke-SelfTest '16 IPv4 gateway is BaselineMismatch' {
    $route = [pscustomobject]@{ InterfaceIndex = 42; DestinationPrefix = '0.0.0.0/0'; NextHop = '10.250.0.254' }
    Assert-True (-not (Get-FakeSnapshot -Data (New-FakeSnapshotData -Routes @($route))).baselineMatch)
}
Invoke-SelfTest '17 IPv4 DNS is BaselineMismatch' {
    Assert-True (-not (Get-FakeSnapshot -Data (New-FakeSnapshotData -Ipv4Dns @('1.1.1.1'))).baselineMatch)
}
Invoke-SelfTest '18 complete diagnostic markers pass' {
    Assert-True (Invoke-Private -Name 'Test-DiagnosticExecutionProof' -Arguments @{ ProcessResult = [pscustomobject]@{ ExitCode = 0; Output = (New-GoodDiagnosticOutput) }; AdapterGuid = $script:TargetGuid })
}
Invoke-SelfTest '19 missing diagnostic marker fails' {
    $output = (New-GoodDiagnosticOutput) -replace 'DnsClientCimIPv6:', ''
    Assert-FailureCode 'DiagnosticExecutionNotProven' { Invoke-Private -Name 'Test-DiagnosticExecutionProof' -Arguments @{ ProcessResult = [pscustomobject]@{ ExitCode = 0; Output = $output }; AdapterGuid = $script:TargetGuid } }
}
Invoke-SelfTest '20 skipped or no-test diagnostic fails' {
    $output = (New-GoodDiagnosticOutput) + "`nNo test matches the given testcase filter."
    Assert-FailureCode 'DiagnosticSkipped' { Invoke-Private -Name 'Test-DiagnosticExecutionProof' -Arguments @{ ProcessResult = [pscustomobject]@{ ExitCode = 0; Output = $output }; AdapterGuid = $script:TargetGuid } }
}
Invoke-SelfTest '21 diagnostic GUID mismatch fails' {
    Assert-FailureCode 'DiagnosticGuidMismatch' { Invoke-Private -Name 'Test-DiagnosticExecutionProof' -Arguments @{ ProcessResult = [pscustomobject]@{ ExitCode = 0; Output = (New-GoodDiagnosticOutput -Guid $script:OtherGuid) }; AdapterGuid = $script:TargetGuid } }
}
Invoke-SelfTest '22 identical snapshots pass equality' {
    $before = Get-FakeSnapshot -Data (New-FakeSnapshotData)
    $after = Get-FakeSnapshot -Data (New-FakeSnapshotData)
    Assert-True (Invoke-Private -Name 'Test-HostLabSnapshotsEqual' -Arguments @{ Before = $before; After = $after })
}
Invoke-SelfTest '23 IPv4 state change fails equality' {
    $before = Get-FakeSnapshot -Data (New-FakeSnapshotData)
    $after = Get-FakeSnapshot -Data (New-FakeSnapshotData -Ipv4 '10.250.0.2')
    Assert-True (-not (Invoke-Private -Name 'Test-HostLabSnapshotsEqual' -Arguments @{ Before = $before; After = $after }))
}
Invoke-SelfTest '24 IPv6 DNS change fails equality' {
    $before = Get-FakeSnapshot -Data (New-FakeSnapshotData)
    $after = Get-FakeSnapshot -Data (New-FakeSnapshotData -Ipv6Dns @('fe80::1'))
    Assert-True (-not (Invoke-Private -Name 'Test-HostLabSnapshotsEqual' -Arguments @{ Before = $before; After = $after }))
}
Invoke-SelfTest '25 route ownership change fails equality' {
    $before = Get-FakeSnapshot -Data (New-FakeSnapshotData)
    $route = [pscustomobject]@{ InterfaceIndex = 42; DestinationPrefix = '::/0'; NextHop = 'fe80::2' }
    $after = Get-FakeSnapshot -Data (New-FakeSnapshotData -Routes @($route))
    Assert-True (-not (Invoke-Private -Name 'Test-HostLabSnapshotsEqual' -Arguments @{ Before = $before; After = $after }))
}
Invoke-SelfTest '26 child IPMAN environment is cleared and constrained' {
    $original = [Environment]::GetEnvironmentVariable('IPMAN_SELF_TEST_SENTINEL', 'Process')
    try {
        [Environment]::SetEnvironmentVariable('IPMAN_SELF_TEST_SENTINEL', 'must-not-leak', 'Process')
        $info = Invoke-Private -Name 'New-DiagnosticProcessStartInfo' -Arguments @{ RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path; AdapterGuid = $script:TargetGuid }
        Assert-True (-not $info.EnvironmentVariables.ContainsKey('IPMAN_SELF_TEST_SENTINEL'))
        Assert-Equal '1' $info.EnvironmentVariables['IPMAN_DNS_TRUTH_DIAGNOSTIC']
        Assert-Equal $script:TargetGuid $info.EnvironmentVariables['IPMAN_TEST_ADAPTER_ID']
    }
    finally { [Environment]::SetEnvironmentVariable('IPMAN_SELF_TEST_SENTINEL', $original, 'Process') }
}
Invoke-SelfTest '27 read-only orchestration keeps mutation disallowed' {
    $fake = New-FakeOperations
    $result = Invoke-FakeOrchestration -Action 'DiscoverTarget' -FakeOperations $fake
    Assert-True (-not $result.Summary.mutationAllowed)
}
Invoke-SelfTest '28 read-only orchestration keeps mutation unperformed' {
    $fake = New-FakeOperations
    $result = Invoke-FakeOrchestration -Action 'DiscoverTarget' -FakeOperations $fake
    Assert-True (-not $result.Summary.mutationPerformed)
}
Invoke-SelfTest '29 public action list has no generic bypass' {
    $command = Get-Command Invoke-IPManHostLab
    $validateSet = $command.Parameters['Action'].Attributes | Where-Object { $_ -is [System.Management.Automation.ValidateSetAttribute] }
    $actions = @($validateSet.ValidValues)
    Assert-Equal 'CollectEvidence,ControlledValidation,DiscoverTarget,DnsTruthDiagnostic,Preflight' (($actions | Sort-Object) -join ',')
    Assert-True ($actions -notcontains 'Command' -and $actions -notcontains 'ScriptBlock')
}
Invoke-SelfTest '30 evidence paths are deterministic in shape and unique' {
    $root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
    $time = [DateTime]::Parse('2026-08-16T12:34:56.789Z').ToUniversalTime()
    $one = Invoke-Private -Name 'New-EvidenceDirectoryPath' -Arguments @{ RepoRoot = $root; Action = 'DiscoverTarget'; RunId = 'run-one'; UtcNow = $time }
    $two = Invoke-Private -Name 'New-EvidenceDirectoryPath' -Arguments @{ RepoRoot = $root; Action = 'DiscoverTarget'; RunId = 'run-two'; UtcNow = $time }
    Assert-True ($one.EndsWith('artifacts\host-lab\20260816T123456789Z-discovertarget-run-one'))
    Assert-True ($one -cne $two)
}
Invoke-SelfTest '31 fresh snapshot GUID missing fails closed' {
    Assert-FailureCode 'SnapshotAdapterNotFound' { Get-FakeSnapshot -Data (New-FakeSnapshotData -NetAdapters @()) }
}
Invoke-SelfTest '32 duplicate fresh snapshot GUID fails closed' {
    $duplicates = @((New-FakeAdapter), (New-FakeAdapter -Name 'duplicate' -Index 43))
    Assert-FailureCode 'SnapshotAdapterAmbiguous' { Get-FakeSnapshot -Data (New-FakeSnapshotData -NetAdapters $duplicates) }
}
Invoke-SelfTest '33 fresh InterfaceIndex rebind fails closed' {
    Assert-FailureCode 'SnapshotIdentityDrift' { Get-FakeSnapshot -Data (New-FakeSnapshotData -NetAdapters @((New-FakeAdapter -Index 43))) }
}
Invoke-SelfTest '34 missing fresh identity source fails closed' {
    $data = [pscustomobject]@{ IPAddresses = @(); Routes = @(); Dns = @() }
    Assert-FailureCode 'SnapshotIdentityReadFailed' { Get-FakeSnapshot -Data $data }
}
Invoke-SelfTest '35 orchestration reports BaselineMismatch' {
    $fake = New-FakeOperations -Snapshots @((New-FakeSnapshotData -Ipv4 '10.250.0.2'))
    $result = Invoke-FakeOrchestration -Action 'DiscoverTarget' -FakeOperations $fake
    Assert-Equal 'BaselineMismatch' $result.FailureCode
    Assert-Equal 0 $fake.Calls.Diagnostic
}
Invoke-SelfTest '36 non-zero diagnostic plus state change prioritizes state change' {
    $fake = New-FakeOperations -Snapshots @((New-FakeSnapshotData), (New-FakeSnapshotData -Ipv4 '10.250.0.2')) -DiagnosticResult ([pscustomobject]@{ ExitCode = 9; Output = 'failed' })
    $result = Invoke-FakeOrchestration -Action 'DnsTruthDiagnostic' -FakeOperations $fake
    Assert-Equal 'ReadOnlyDiagnosticStateChanged' $result.FailureCode
    Assert-Equal 2 $fake.Calls.Snapshot
}
Invoke-SelfTest '37 marker failure plus state change prioritizes state change' {
    $badProof = [pscustomobject]@{ ExitCode = 0; Output = ((New-GoodDiagnosticOutput) -replace 'DnsClientCimIPv6:', '') }
    $fake = New-FakeOperations -Snapshots @((New-FakeSnapshotData), (New-FakeSnapshotData -Ipv4 '10.250.0.2')) -DiagnosticResult $badProof
    $result = Invoke-FakeOrchestration -Action 'DnsTruthDiagnostic' -FakeOperations $fake
    Assert-Equal 'ReadOnlyDiagnosticStateChanged' $result.FailureCode
}
Invoke-SelfTest '38 diagnostic failure with unchanged state reports diagnostic failure' {
    $fake = New-FakeOperations -DiagnosticResult ([pscustomobject]@{ ExitCode = 7; Output = 'failed' })
    $result = Invoke-FakeOrchestration -Action 'DnsTruthDiagnostic' -FakeOperations $fake
    Assert-Equal 'DiagnosticFailed' $result.FailureCode
    Assert-True $result.Summary.beforeAfterEqual
}
Invoke-SelfTest '39 AFTER snapshot read failure follows diagnostic execution' {
    $failure = [pscustomobject]@{ ReadFailure = 'simulated AFTER read failure' }
    $fake = New-FakeOperations -Snapshots @((New-FakeSnapshotData), $failure)
    $result = Invoke-FakeOrchestration -Action 'DnsTruthDiagnostic' -FakeOperations $fake
    Assert-Equal 'AfterSnapshotReadFailed' $result.FailureCode
    Assert-Equal 1 $fake.Calls.Diagnostic
    Assert-Equal 2 $fake.Calls.Snapshot
}
Invoke-SelfTest '40 orchestration detects fresh AFTER identity drift' {
    $drifted = New-FakeSnapshotData -NetAdapters @((New-FakeAdapter -Index 43))
    $fake = New-FakeOperations -Snapshots @((New-FakeSnapshotData), $drifted)
    $result = Invoke-FakeOrchestration -Action 'DnsTruthDiagnostic' -FakeOperations $fake
    Assert-Equal 'SnapshotIdentityDrift' $result.FailureCode
}
Invoke-SelfTest '41 marker failure is evaluated after unchanged state proof' {
    $badProof = [pscustomobject]@{ ExitCode = 0; Output = ((New-GoodDiagnosticOutput) -replace 'DnsClientCimIPv6:', '') }
    $fake = New-FakeOperations -DiagnosticResult $badProof
    $result = Invoke-FakeOrchestration -Action 'DnsTruthDiagnostic' -FakeOperations $fake
    Assert-Equal 'DiagnosticExecutionNotProven' $result.FailureCode
    Assert-True $result.Summary.beforeAfterEqual
}
Invoke-SelfTest '42 orchestration exercises all injected boundaries' {
    $fake = New-FakeOperations
    $result = Invoke-FakeOrchestration -Action 'DnsTruthDiagnostic' -FakeOperations $fake
    Assert-True $result.Success
    Assert-Equal 1 $fake.Calls.Prerequisites
    Assert-Equal 1 $fake.Calls.Provenance
    Assert-Equal 1 $fake.Calls.Discovery
    Assert-Equal 2 $fake.Calls.Snapshot
    Assert-Equal 1 $fake.Calls.Diagnostic
    Assert-Equal 1 $fake.Calls.Evidence
}
Invoke-SelfTest '43 fresh MAC rebind fails closed' {
    $rebound = New-FakeSnapshotData -NetAdapters @((New-FakeAdapter -Mac '00-15-5D-99-99-99'))
    Assert-FailureCode 'SnapshotIdentityDrift' { Get-FakeSnapshot -Data $rebound }
}
Invoke-SelfTest '44 DNS and IPv4 mutations succeed and both roll back' {
    $snapshots = @(
        (New-FakeSnapshotData),
        (New-FakeSnapshotData -Ipv4Dns @('10.250.0.53')),
        (New-FakeSnapshotData),
        (New-FakeSnapshotData -Ipv4 '10.250.0.2'),
        (New-FakeSnapshotData)
    )
    $fake = New-FakeOperations -Snapshots $snapshots
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations $fake
    Assert-True $result.Success
    Assert-True $result.Summary.beforeAfterEqual
    Assert-True $result.Summary.mutationAllowed
    Assert-True $result.Summary.mutationPerformed
    Assert-Equal 4 $fake.Calls.Scenario
    Assert-Equal 7 $fake.Calls.SaveState
}
Invoke-SelfTest '45 DNS verification failure still rolls back and stops IPv4' {
    $snapshots = @(
        (New-FakeSnapshotData),
        (New-FakeSnapshotData -Ipv4Dns @('1.1.1.1')),
        (New-FakeSnapshotData)
    )
    $fake = New-FakeOperations -Snapshots $snapshots
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations $fake
    Assert-Equal 'MutationVerificationFailed' $result.FailureCode
    Assert-Equal 2 $fake.Calls.Scenario
}
Invoke-SelfTest '46 IPv4 verification failure still rolls back' {
    $snapshots = @(
        (New-FakeSnapshotData),
        (New-FakeSnapshotData -Ipv4Dns @('10.250.0.53')),
        (New-FakeSnapshotData),
        (New-FakeSnapshotData -Ipv4 '10.250.0.3'),
        (New-FakeSnapshotData)
    )
    $fake = New-FakeOperations -Snapshots $snapshots
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations $fake
    Assert-Equal 'MutationVerificationFailed' $result.FailureCode
    Assert-Equal 4 $fake.Calls.Scenario
}
Invoke-SelfTest '47 rollback failure stops all further mutation' {
    $snapshots = @(
        (New-FakeSnapshotData),
        (New-FakeSnapshotData -Ipv4Dns @('10.250.0.53')),
        (New-FakeSnapshotData -Ipv4Dns @('10.250.0.53'))
    )
    $fake = New-FakeOperations -Snapshots $snapshots
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations $fake
    Assert-Equal 'ROLLBACK_FAILED' $result.FailureCode
    Assert-Equal 2 $fake.Calls.Scenario
}
Invoke-SelfTest '48 identity drift after mutation rolls back and fails closed' {
    $snapshots = @(
        (New-FakeSnapshotData),
        (New-FakeSnapshotData -Ipv4Dns @('10.250.0.53') -NetAdapters @((New-FakeAdapter -Index 43))),
        (New-FakeSnapshotData)
    )
    $fake = New-FakeOperations -Snapshots $snapshots
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations $fake
    Assert-Equal 'SnapshotIdentityDrift' $result.FailureCode
    Assert-Equal 2 $fake.Calls.Scenario
}
Invoke-SelfTest '49 production module has no mandatory Hyper-V cmdlet dependency' {
    $source = Get-Content -LiteralPath $modulePath -Raw
    Assert-True ($source -notmatch 'Get-VMSwitch|Get-VMNetworkAdapter')
}
Invoke-SelfTest '50 controlled validation requires Phase-A exact GUID' {
    $fake = New-FakeOperations -Snapshots @((New-FakeSnapshotData))
    $result = Invoke-Private -Name 'Invoke-HostLabOrchestration' -Arguments @{
        Action = 'ControlledValidation'
        RepoRoot = 'C:\deterministic-host-lab-test'
        Operations = $fake.Operations
    }
    Assert-Equal 'ExpectedGuidRequiredForMutation' $result.FailureCode
    Assert-Equal 0 $fake.Calls.Scenario
}
Invoke-SelfTest '51 controlled child environment is exact and isolated' {
    $original = [Environment]::GetEnvironmentVariable('IPMAN_CONTROLLED_SENTINEL', 'Process')
    try {
        [Environment]::SetEnvironmentVariable('IPMAN_CONTROLLED_SENTINEL', 'must-not-leak', 'Process')
        $info = Invoke-Private -Name 'New-DestructiveScenarioProcessStartInfo' -Arguments @{
            RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
            AdapterGuid = $script:TargetGuid
            Scenario = 'ManualDnsOne'
            Ipv4Address = '10.250.0.1'
            DnsAddress = '10.250.0.53'
            EvidenceRoot = 'C:\deterministic-evidence'
        }
        Assert-True (-not $info.EnvironmentVariables.ContainsKey('IPMAN_CONTROLLED_SENTINEL'))
        Assert-Equal '1' $info.EnvironmentVariables['IPMAN_DESTRUCTIVE_NETWORK_TESTS']
        Assert-Equal $script:TargetGuid $info.EnvironmentVariables['IPMAN_TEST_ADAPTER_ID']
        Assert-Equal 'ManualDnsOne' $info.EnvironmentVariables['IPMAN_TEST_SCENARIO']
        Assert-Equal 'NONE' $info.EnvironmentVariables['IPMAN_TEST_GATEWAY']
    }
    finally { [Environment]::SetEnvironmentVariable('IPMAN_CONTROLLED_SENTINEL', $original, 'Process') }
}

Invoke-SelfTest '52 metric-only non-target default-route drift is evidence, not failure' {
    $adapters = @((New-FakeAdapter), (New-FakeNonTargetAdapter))
    $snapshots = @(
        (New-FakeSnapshotData -NetAdapters $adapters -Routes @((New-FakeDefaultRoute -InterfaceMetric 40))),
        (New-FakeSnapshotData -Ipv4Dns @('10.250.0.53') -NetAdapters $adapters -Routes @((New-FakeDefaultRoute -InterfaceMetric 45))),
        (New-FakeSnapshotData -NetAdapters $adapters -Routes @((New-FakeDefaultRoute -InterfaceMetric 50))),
        (New-FakeSnapshotData -Ipv4 '10.250.0.2' -NetAdapters $adapters -Routes @((New-FakeDefaultRoute -RouteMetric 5 -InterfaceMetric 50))),
        (New-FakeSnapshotData -NetAdapters $adapters -Routes @((New-FakeDefaultRoute -InterfaceMetric 45)))
    )
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations (New-FakeOperations -Snapshots $snapshots)
    Assert-True $result.Success
    Assert-True (@($result.Summary.nonTargetMetricDrift).Count -gt 0)
}

Invoke-SelfTest '53 non-target next-hop drift fails closed without rollback misclassification' {
    $adapters = @((New-FakeAdapter), (New-FakeNonTargetAdapter))
    $snapshots = @(
        (New-FakeSnapshotData -NetAdapters $adapters -Routes @((New-FakeDefaultRoute))),
        (New-FakeSnapshotData -Ipv4Dns @('10.250.0.53') -NetAdapters $adapters -Routes @((New-FakeDefaultRoute -NextHop '192.168.1.254'))),
        (New-FakeSnapshotData -NetAdapters $adapters -Routes @((New-FakeDefaultRoute)))
    )
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations (New-FakeOperations -Snapshots $snapshots)
    Assert-Equal 'NON_TARGET_STATE_CHANGED' $result.FailureCode
}

Invoke-SelfTest '54 non-target default-route removal fails closed' {
    $adapters = @((New-FakeAdapter), (New-FakeNonTargetAdapter))
    $snapshots = @(
        (New-FakeSnapshotData -NetAdapters $adapters -Routes @((New-FakeDefaultRoute))),
        (New-FakeSnapshotData -Ipv4Dns @('10.250.0.53') -NetAdapters $adapters -Routes @()),
        (New-FakeSnapshotData -NetAdapters $adapters -Routes @((New-FakeDefaultRoute)))
    )
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations (New-FakeOperations -Snapshots $snapshots)
    Assert-Equal 'NON_TARGET_STATE_CHANGED' $result.FailureCode
}

Invoke-SelfTest '55 non-target DNS drift fails closed' {
    $adapters = @((New-FakeAdapter), (New-FakeNonTargetAdapter))
    $baselineDns = [pscustomobject]@{ InterfaceIndex = 7; AddressFamily = 'IPv4'; ServerAddresses = @('192.168.1.1') }
    $changedDns = [pscustomobject]@{ InterfaceIndex = 7; AddressFamily = 'IPv4'; ServerAddresses = @('1.1.1.1') }
    $snapshots = @(
        (New-FakeSnapshotData -NetAdapters $adapters -ExtraDns @($baselineDns)),
        (New-FakeSnapshotData -Ipv4Dns @('10.250.0.53') -NetAdapters $adapters -ExtraDns @($changedDns)),
        (New-FakeSnapshotData -NetAdapters $adapters -ExtraDns @($baselineDns))
    )
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations (New-FakeOperations -Snapshots $snapshots)
    Assert-Equal 'NON_TARGET_STATE_CHANGED' $result.FailureCode
}

Invoke-SelfTest '56 non-target address drift fails closed' {
    $adapters = @((New-FakeAdapter), (New-FakeNonTargetAdapter))
    $baselineAddress = [pscustomobject]@{ InterfaceIndex = 7; AddressFamily = 'IPv4'; IPAddress = '192.168.1.10'; PrefixLength = 24 }
    $changedAddress = [pscustomobject]@{ InterfaceIndex = 7; AddressFamily = 'IPv4'; IPAddress = '192.168.1.11'; PrefixLength = 24 }
    $snapshots = @(
        (New-FakeSnapshotData -NetAdapters $adapters -ExtraAddresses @($baselineAddress)),
        (New-FakeSnapshotData -Ipv4Dns @('10.250.0.53') -NetAdapters $adapters -ExtraAddresses @($changedAddress)),
        (New-FakeSnapshotData -NetAdapters $adapters -ExtraAddresses @($baselineAddress))
    )
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations (New-FakeOperations -Snapshots $snapshots)
    Assert-Equal 'NON_TARGET_STATE_CHANGED' $result.FailureCode
}

Invoke-SelfTest '57 failed apply with unchanged target skips redundant rollback' {
    $failedProcess = [pscustomobject]@{ ExitCode = 5; Output = 'apply failed'; Scenario = 'ManualDnsOne'; ProcessStarted = $true }
    $fake = New-FakeOperations -Snapshots @((New-FakeSnapshotData), (New-FakeSnapshotData)) -ScenarioResults @($failedProcess)
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations $fake
    Assert-Equal 'MutationHarnessFailed' $result.FailureCode
    Assert-Equal 1 $fake.Calls.Scenario
    Assert-True $result.Summary.mutationAttempted
    Assert-True (-not $result.Summary.mutationPerformed)
}

Invoke-SelfTest '58 failed apply with changed target invokes rollback' {
    $failedProcess = [pscustomobject]@{ ExitCode = 5; Output = 'apply failed'; Scenario = 'ManualDnsOne'; ProcessStarted = $true }
    $snapshots = @(
        (New-FakeSnapshotData),
        (New-FakeSnapshotData -Ipv4Dns @('10.250.0.53')),
        (New-FakeSnapshotData)
    )
    $fake = New-FakeOperations -Snapshots $snapshots -ScenarioResults @($failedProcess)
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations $fake
    Assert-Equal 'MutationHarnessFailed' $result.FailureCode
    Assert-Equal 2 $fake.Calls.Scenario
    Assert-True $result.Summary.mutationAttempted
    Assert-True $result.Summary.mutationPerformed
}

Invoke-SelfTest '59 non-elevated controlled validation stops before scenario launch' {
    $fake = New-FakeOperations -Snapshots @((New-FakeSnapshotData)) -Elevated $false
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations $fake
    Assert-Equal 'ELEVATION_REQUIRED_FOR_MUTATION' $result.FailureCode
    Assert-Equal 0 $fake.Calls.Scenario
    Assert-True (-not $result.Summary.mutationAttempted)
    Assert-True (-not $result.Summary.mutationPerformed)
}

Invoke-SelfTest '60 non-target default-route addition fails closed' {
    $adapters = @((New-FakeAdapter), (New-FakeNonTargetAdapter))
    $snapshots = @(
        (New-FakeSnapshotData -NetAdapters $adapters -Routes @()),
        (New-FakeSnapshotData -Ipv4Dns @('10.250.0.53') -NetAdapters $adapters -Routes @((New-FakeDefaultRoute))),
        (New-FakeSnapshotData -NetAdapters $adapters -Routes @())
    )
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations (New-FakeOperations -Snapshots $snapshots)
    Assert-Equal 'NON_TARGET_STATE_CHANGED' $result.FailureCode
}

Invoke-SelfTest '61 diagnostic accepts exact canonical GUID with braces' {
    $output = (New-GoodDiagnosticOutput) -replace "ExactAdapterId: $script:TargetGuid", "ExactAdapterId: {$script:TargetGuid}"
    $result = Invoke-Private -Name 'Test-DiagnosticExecutionProof' -Arguments @{
        ProcessResult = [pscustomobject]@{ ExitCode = 0; Output = $output }
        AdapterGuid = $script:TargetGuid
    }
    Assert-True $result
}

Invoke-SelfTest '62 live numeric DNS address families are captured exactly' {
    $snapshot = Get-FakeSnapshot -Data (New-FakeSnapshotData -Ipv4Dns @('10.250.0.53') -Ipv6Dns @('2001:db8::53') -Ipv4DnsFamily 2 -Ipv6DnsFamily 23)
    Assert-Equal '10.250.0.53' (@($snapshot.ipv4DnsServerAddresses) -join '|')
    Assert-Equal '2001:db8::53' (@($snapshot.ipv6DnsServerAddresses) -join '|')
}

Invoke-SelfTest '63 target safety failure outranks non-zero harness exit after rollback' {
    $failedProcess = [pscustomobject]@{ ExitCode = 5; Output = 'apply failed'; Scenario = 'ManualDnsOne'; ProcessStarted = $true }
    $targetRoute = [pscustomobject]@{ InterfaceIndex = 42; DestinationPrefix = '0.0.0.0/0'; NextHop = '10.250.0.254'; RouteMetric = 0; InterfaceMetric = 15 }
    $snapshots = @(
        (New-FakeSnapshotData),
        (New-FakeSnapshotData -Routes @($targetRoute)),
        (New-FakeSnapshotData)
    )
    $fake = New-FakeOperations -Snapshots $snapshots -ScenarioResults @($failedProcess)
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations $fake
    Assert-Equal 'DefaultRouteCreated' $result.FailureCode
    Assert-Equal 2 $fake.Calls.Scenario
}

Invoke-SelfTest '64 process evidence preserves exit code and separated streams' {
    $evidence = Invoke-Private -Name 'Format-ControlledProcessEvidence' -Arguments @{
        ProcessResult = [pscustomobject]@{ ProcessStarted = $true; ExitCode = 1; Stdout = 'stdout-value'; Stderr = 'stderr-value' }
        ProcessError = $null
        Scenario = 'ManualDnsOne'
    }
    Assert-True ($evidence -match 'ExitCode: 1')
    Assert-True ($evidence -match '(?s)--- STDOUT ---.*stdout-value')
    Assert-True ($evidence -match '(?s)--- STDERR ---.*stderr-value')
}

Invoke-SelfTest '65 IPv6 sentinel empty versus exact trio is equivalent and raw values remain captured' {
    $trio = @('fec0:0:0:ffff::1', 'fec0:0:0:ffff::2', 'fec0:0:0:ffff::3')
    $before = Get-FakeSnapshot -Data (New-FakeSnapshotData)
    $after = Get-FakeSnapshot -Data (New-FakeSnapshotData -Ipv6Dns $trio)
    Assert-True (Invoke-Private -Name 'Test-HostLabSnapshotsEqual' -Arguments @{ Before = $before; After = $after })
    Assert-Equal ($trio -join '|') (@($after.ipv6DnsServerAddresses) -join '|')
}

Invoke-SelfTest '66 IPv6 sentinel exact trio versus empty is equivalent' {
    $trio = @('fec0:0:0:ffff::1', 'fec0:0:0:ffff::2', 'fec0:0:0:ffff::3')
    $before = Get-FakeSnapshot -Data (New-FakeSnapshotData -Ipv6Dns $trio)
    $after = Get-FakeSnapshot -Data (New-FakeSnapshotData)
    Assert-True (Invoke-Private -Name 'Test-HostLabSnapshotsEqual' -Arguments @{ Before = $before; After = $after })
}

Invoke-SelfTest '67 IPv6 sentinel zone-suffixed exact trio is equivalent to empty' {
    $trio = @('fec0:0:0:ffff::1%42', 'fec0:0:0:ffff::2%IPMan-Test-Switch', 'fec0:0:0:ffff::3%vEthernet (IPMan-Test-Switch)')
    Assert-True (Invoke-Private -Name 'Test-Ipv6DnsSemanticallyUnconfigured' -Arguments @{ Values = $trio }) 'Zone-suffixed trio was not recognized as semantically unconfigured.'
    $before = Get-FakeSnapshot -Data (New-FakeSnapshotData)
    $after = Get-FakeSnapshot -Data (New-FakeSnapshotData -Ipv6Dns $trio)
    Assert-True (Invoke-Private -Name 'Test-HostLabSnapshotsEqual' -Arguments @{ Before = $before; After = $after })
}

Invoke-SelfTest '68 IPv6 sentinel partial trio is not equivalent to empty' {
    $before = Get-FakeSnapshot -Data (New-FakeSnapshotData)
    $after = Get-FakeSnapshot -Data (New-FakeSnapshotData -Ipv6Dns @('fec0:0:0:ffff::1', 'fec0:0:0:ffff::2'))
    Assert-True (-not (Invoke-Private -Name 'Test-HostLabSnapshotsEqual' -Arguments @{ Before = $before; After = $after }))
}

Invoke-SelfTest '69 IPv6 sentinel trio plus real DNS is not equivalent to empty' {
    $dns = @('fec0:0:0:ffff::1', 'fec0:0:0:ffff::2', 'fec0:0:0:ffff::3', '2001:4860:4860::8888')
    $before = Get-FakeSnapshot -Data (New-FakeSnapshotData)
    $after = Get-FakeSnapshot -Data (New-FakeSnapshotData -Ipv6Dns $dns)
    Assert-True (-not (Invoke-Private -Name 'Test-HostLabSnapshotsEqual' -Arguments @{ Before = $before; After = $after }))
}

Invoke-SelfTest '70 IPv6 sentinel real DNS change fails controlled mutation safety' {
    $before = Get-FakeSnapshot -Data (New-FakeSnapshotData)
    $after = Get-FakeSnapshot -Data (New-FakeSnapshotData -Ipv6Dns @('2001:4860:4860::8888'))
    Assert-FailureCode 'MutationVerificationFailed' { Invoke-Private -Name 'Assert-ControlledMutationSafety' -Arguments @{ Baseline = $before; Observed = $after } }
}

Invoke-SelfTest '71 IPv6 sentinel address change fails controlled mutation safety' {
    $before = Get-FakeSnapshot -Data (New-FakeSnapshotData)
    $after = Get-FakeSnapshot -Data (New-FakeSnapshotData -Ipv6 'fe80::2')
    Assert-FailureCode 'MutationVerificationFailed' { Invoke-Private -Name 'Assert-ControlledMutationSafety' -Arguments @{ Baseline = $before; Observed = $after } }
}

Invoke-SelfTest '72 IPv6 sentinel route change fails controlled mutation safety' {
    $route = [pscustomobject]@{ InterfaceIndex = 42; DestinationPrefix = 'fe80::/64'; NextHop = '::'; RouteMetric = 0; InterfaceMetric = 25 }
    $before = Get-FakeSnapshot -Data (New-FakeSnapshotData)
    $after = Get-FakeSnapshot -Data (New-FakeSnapshotData -Routes @($route))
    Assert-FailureCode 'MutationVerificationFailed' { Invoke-Private -Name 'Assert-ControlledMutationSafety' -Arguments @{ Baseline = $before; Observed = $after } }
}

Invoke-SelfTest '73 IPv6 sentinel final rollback equality uses semantic comparison' {
    $trio = @('fec0:0:0:ffff::1', 'fec0:0:0:ffff::2', 'fec0:0:0:ffff::3')
    $snapshots = @(
        (New-FakeSnapshotData),
        (New-FakeSnapshotData -Ipv4Dns @('10.250.0.53') -Ipv6Dns $trio),
        (New-FakeSnapshotData -Ipv6Dns $trio),
        (New-FakeSnapshotData -Ipv4 '10.250.0.2' -Ipv6Dns $trio),
        (New-FakeSnapshotData -Ipv6Dns $trio)
    )
    $result = Invoke-FakeOrchestration -Action 'ControlledValidation' -FakeOperations (New-FakeOperations -Snapshots $snapshots)
    Assert-True $result.Success
    Assert-True $result.Summary.beforeAfterEqual
}

Write-Output "RESULT Passed=$script:Passed Failed=$script:Failed Total=$($script:Passed + $script:Failed)"
if ($script:Failures.Count -gt 0) {
    $script:Failures | ForEach-Object { Write-Output "  $_" }
    exit 1
}
