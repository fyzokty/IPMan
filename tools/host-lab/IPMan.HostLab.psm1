Set-StrictMode -Version 2.0

$script:HostLabSchemaVersion = 1
$script:DefaultSwitchName = 'IPMan-Test-Switch'
$script:DefaultAliasHint = 'vEthernet (IPMan-Test-Switch)'
$script:ExpectedIpv4Address = '10.250.0.1'
$script:ExpectedIpv4PrefixLength = 24
$script:ValidationIpv4Address = '10.250.0.2'
$script:ValidationDnsAddress = '10.250.0.53'
$script:DiagnosticMarkers = @(
    'DnsTruthDiagnostic: Enabled',
    'MutationPerformed: False',
    'GetInterfaceDnsSettings',
    'DnsClientCimIPv4',
    'DnsClientCimIPv6',
    'GetAdaptersAddressesIPv4',
    'GetAdaptersAddressesIPv6'
)

function New-HostLabException {
    param(
        [Parameter(Mandatory = $true)][string]$Code,
        [Parameter(Mandatory = $true)][string]$Message
    )

    $exception = New-Object System.InvalidOperationException($Message)
    $exception.Data['HostLabFailureCode'] = $Code
    return $exception
}

function Throw-HostLabFailure {
    param(
        [Parameter(Mandatory = $true)][string]$Code,
        [Parameter(Mandatory = $true)][string]$Message
    )

    throw (New-HostLabException -Code $Code -Message $Message)
}

function ConvertTo-CanonicalGuid {
    param([Parameter(Mandatory = $true)]$Value)

    $parsed = [Guid]::Empty
    if (-not [Guid]::TryParse(([string]$Value).Trim('{}'), [ref]$parsed)) {
        Throw-HostLabFailure -Code 'InvalidAdapterGuid' -Message "Invalid adapter GUID: $Value"
    }

    return $parsed.ToString('D').ToUpperInvariant()
}

function ConvertTo-NormalizedMac {
    param($Value)

    if ($null -eq $Value) { return '' }
    return ([string]$Value -replace '[:-]', '').ToUpperInvariant()
}

function Get-PropertyValue {
    param(
        [Parameter(Mandatory = $true)]$InputObject,
        [Parameter(Mandatory = $true)][string]$Name,
        $DefaultValue = $null
    )

    $property = $InputObject.PSObject.Properties[$Name]
    if ($null -eq $property) { return $DefaultValue }
    return $property.Value
}

function ConvertTo-StableStringArray {
    param($Values)

    [string[]]$result = @($Values | ForEach-Object { [string]$_ } | Sort-Object -Unique)
    return ,$result
}

function Test-Ipv6DnsSemanticallyUnconfigured {
    param($Values)

    [string[]]$actual = ConvertTo-StableStringArray -Values $Values
    if ($actual.Count -eq 0) { return $true }
    if ($actual.Count -ne 3) { return $false }

    $withoutScopes = New-Object System.Collections.Generic.HashSet[string]([StringComparer]::OrdinalIgnoreCase)
    foreach ($item in $actual) {
        $value = [string]$item
        $match = [regex]::Match($value, '^(?<address>fec0:0:0:ffff::[123])(?:%(?<scope>\w[-\w.() ]*))?$', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
        if (-not $match.Success) { return $false }
        $withoutScopes.Add($match.Groups['address'].Value) | Out-Null
    }

    [string[]]$expected = @(
        'fec0:0:0:ffff::1',
        'fec0:0:0:ffff::2',
        'fec0:0:0:ffff::3'
    )
    return $withoutScopes.Count -eq $expected.Count -and
        @($expected | Where-Object { -not $withoutScopes.Contains($_) }).Count -eq 0
}

function Test-Ipv6DnsServerAddressesEqual {
    param(
        $Before,
        $After
    )

    if ((Test-Ipv6DnsSemanticallyUnconfigured -Values $Before) -and
        (Test-Ipv6DnsSemanticallyUnconfigured -Values $After)) {
        return $true
    }

    [string[]]$beforeStable = ConvertTo-StableStringArray -Values $Before
    [string[]]$afterStable = ConvertTo-StableStringArray -Values $After
    return ($beforeStable -join '|') -ceq ($afterStable -join '|')
}

function Test-AddressFamilyValue {
    param(
        [Parameter(Mandatory = $true)]$Value,
        [Parameter(Mandatory = $true)][ValidateSet('IPv4', 'IPv6')][string]$Expected
    )

    $actual = [string]$Value
    if ($Expected -eq 'IPv4') {
        return $actual -in @('IPv4', 'InterNetwork', '2')
    }
    return $actual -in @('IPv6', 'InterNetworkV6', '23')
}

function Test-IsDefaultRoute {
    param($Route)

    $destination = [string](Get-PropertyValue -InputObject $Route -Name 'DestinationPrefix' -DefaultValue '')
    return $destination -eq '0.0.0.0/0' -or $destination -eq '::/0'
}

function Test-IsWifiAdapter {
    param($Adapter)

    $ifType = [int](Get-PropertyValue -InputObject $Adapter -Name 'ifType' -DefaultValue 0)
    $mediaType = [string](Get-PropertyValue -InputObject $Adapter -Name 'MediaType' -DefaultValue '')
    $physicalMedium = [string](Get-PropertyValue -InputObject $Adapter -Name 'NdisPhysicalMedium' -DefaultValue '')
    return $ifType -eq 71 -or $mediaType -match '802\.11|Wireless' -or $physicalMedium -match '802\.11|Wireless'
}

function Test-IsPhysicalAdapter {
    param($Adapter)

    $hardwareInterface = [bool](Get-PropertyValue -InputObject $Adapter -Name 'HardwareInterface' -DefaultValue $false)
    $virtual = [bool](Get-PropertyValue -InputObject $Adapter -Name 'Virtual' -DefaultValue $false)
    return $hardwareInterface -or -not $virtual
}

function Resolve-HostLabTarget {
    param(
        [Parameter(Mandatory = $true)]$Data,
        [string]$SwitchName = $script:DefaultSwitchName,
        [string]$AliasHint = $script:DefaultAliasHint,
        [string]$ExpectedAdapterGuid
    )

    $netAdapters = @($Data.NetAdapters)
    if ($ExpectedAdapterGuid) {
        $canonicalExpected = ConvertTo-CanonicalGuid -Value $ExpectedAdapterGuid
        $candidates = @($netAdapters | Where-Object {
            try { (ConvertTo-CanonicalGuid -Value $_.InterfaceGuid) -eq $canonicalExpected } catch { $false }
        })
        if ($candidates.Count -eq 0) {
            Throw-HostLabFailure -Code 'ExpectedGuidNotFound' -Message "Expected adapter GUID $canonicalExpected was not found."
        }
        if ($candidates.Count -ne 1) {
            Throw-HostLabFailure -Code 'ExpectedGuidAmbiguous' -Message "Expected adapter GUID $canonicalExpected was not unique."
        }
    }
    else {
        $candidates = @($netAdapters | Where-Object { [string]$_.Name -eq $AliasHint })
        if ($candidates.Count -eq 0) {
            Throw-HostLabFailure -Code 'AliasHintNotFound' -Message "Adapter alias hint '$AliasHint' was not found."
        }
        if ($candidates.Count -ne 1) {
            Throw-HostLabFailure -Code 'AliasHintAmbiguous' -Message "Adapter alias hint '$AliasHint' was not unique."
        }
    }

    $target = $candidates[0]
    if ([string]$target.Name -cne $AliasHint) {
        Throw-HostLabFailure -Code 'TargetAliasMismatch' -Message 'Exact GUID did not resolve to the required direct HOST vEthernet alias.'
    }
    $targetGuid = ConvertTo-CanonicalGuid -Value $target.InterfaceGuid
    $targetMac = ConvertTo-NormalizedMac -Value $target.MacAddress
    if ([string]::IsNullOrWhiteSpace($targetMac)) {
        Throw-HostLabFailure -Code 'TargetMacMissing' -Message 'Target adapter has no MAC address for stable direct identity proof.'
    }

    $sameMacAdapters = @($netAdapters | Where-Object {
        (ConvertTo-NormalizedMac -Value $_.MacAddress) -eq $targetMac
    })
    if ($sameMacAdapters.Count -ne 1) {
        Throw-HostLabFailure -Code 'TargetIdentityAmbiguous' -Message 'Target MAC does not identify exactly one Windows adapter.'
    }

    $targetIndex = [int]$target.ifIndex
    $targetRoutes = @($Data.Routes | Where-Object { [int]$_.InterfaceIndex -eq $targetIndex -and (Test-IsDefaultRoute $_) })
    $ownsIpv4Default = @($targetRoutes | Where-Object { [string]$_.DestinationPrefix -eq '0.0.0.0/0' }).Count -gt 0
    $ownsIpv6Default = @($targetRoutes | Where-Object { [string]$_.DestinationPrefix -eq '::/0' }).Count -gt 0
    $isWifi = Test-IsWifiAdapter -Adapter $target
    $isPhysical = Test-IsPhysicalAdapter -Adapter $target

    if ($ownsIpv4Default) {
        Throw-HostLabFailure -Code 'TargetOwnsIpv4DefaultRoute' -Message 'Target owns an active IPv4 default route.'
    }
    if ($ownsIpv6Default) {
        Throw-HostLabFailure -Code 'TargetOwnsIpv6DefaultRoute' -Message 'Target owns an active IPv6 default route.'
    }
    if ($isWifi) {
        Throw-HostLabFailure -Code 'TargetIsWifi' -Message 'Target is a Wi-Fi adapter.'
    }
    if ($isPhysical) {
        Throw-HostLabFailure -Code 'TargetIsPhysical' -Message 'Target is a physical or non-virtual adapter.'
    }
    $status = [string](Get-PropertyValue -InputObject $target -Name 'Status' -DefaultValue '')
    if ($status -ne 'Up') {
        Throw-HostLabFailure -Code 'TargetNotUp' -Message 'Target adapter is not Up and usable for controlled validation.'
    }
    if ([string]$target.InterfaceDescription -notmatch 'Hyper-V.*Virtual Ethernet Adapter') {
        Throw-HostLabFailure -Code 'TargetNotHyperVVirtualAdapter' -Message 'Target description does not identify a Hyper-V virtual Ethernet adapter.'
    }

    $managementRoutes = @($Data.Routes | Where-Object { Test-IsDefaultRoute $_ })
    $managementInterfaces = @($managementRoutes | ForEach-Object {
        $route = $_
        $adapter = @($netAdapters | Where-Object { [int]$_.ifIndex -eq [int]$route.InterfaceIndex }) | Select-Object -First 1
        [pscustomobject][ordered]@{
            interfaceIndex = [int]$route.InterfaceIndex
            interfaceGuid = if ($null -ne $adapter) { ConvertTo-CanonicalGuid -Value $adapter.InterfaceGuid } else { $null }
            interfaceAlias = if ($null -ne $adapter) { [string]$adapter.Name } else { $null }
            destinationPrefix = [string]$route.DestinationPrefix
            nextHop = [string](Get-PropertyValue -InputObject $route -Name 'NextHop' -DefaultValue '')
            routeMetric = [int](Get-PropertyValue -InputObject $route -Name 'RouteMetric' -DefaultValue 0)
            interfaceMetric = [int](Get-PropertyValue -InputObject $route -Name 'InterfaceMetric' -DefaultValue 0)
        }
    })

    return [pscustomobject][ordered]@{
        InterfaceGuid = $targetGuid
        InterfaceIndex = $targetIndex
        InterfaceAlias = [string]$target.Name
        InterfaceDescription = [string]$target.InterfaceDescription
        MacAddress = [string]$target.MacAddress
        IfType = Get-PropertyValue -InputObject $target -Name 'ifType'
        MediaType = [string](Get-PropertyValue -InputObject $target -Name 'MediaType' -DefaultValue '')
        AdapterStatus = $status
        SwitchName = $SwitchName
        SwitchType = 'DeferredCrossValidation'
        HyperVAdapterName = $null
        RelationshipProof = 'Exact local alias plus unique InterfaceGuid/MAC and Windows virtual/non-physical adapter classification'
        OwnsIpv4DefaultRoute = $false
        OwnsIpv6DefaultRoute = $false
        IsWifi = $false
        IsPhysical = $false
        ManagementInterfaces = $managementInterfaces
        SafetyGate = 'Pass'
    }
}

function Get-HostLabLiveData {
    param([string]$SwitchName = $script:DefaultSwitchName)

    try {
        $netAdapters = @(Get-NetAdapter -IncludeHidden -ErrorAction Stop)
        $routes = @(Get-NetRoute -PolicyStore ActiveStore -ErrorAction Stop | Where-Object {
            $_.State -eq 'Alive' -or $null -eq $_.PSObject.Properties['State']
        })
    }
    catch {
        Throw-HostLabFailure -Code 'WindowsNetworkReadFailed' -Message "Windows adapter or route truth could not be read: $($_.Exception.Message)"
    }

    return [pscustomobject]@{
        NetAdapters = $netAdapters
        Routes = $routes
    }
}

function Get-HostLabSnapshot {
    param(
        [Parameter(Mandatory = $true)]$Target,
        [Parameter(Mandatory = $true)]$Data
    )

    $netAdaptersProperty = $Data.PSObject.Properties['NetAdapters']
    if ($null -eq $netAdaptersProperty) {
        Throw-HostLabFailure -Code 'SnapshotIdentityReadFailed' -Message 'Fresh Windows adapter identity truth was not supplied to the snapshot.'
    }

    $expectedGuid = ConvertTo-CanonicalGuid -Value $Target.InterfaceGuid
    $identityMatches = @($netAdaptersProperty.Value | Where-Object {
        try { (ConvertTo-CanonicalGuid -Value $_.InterfaceGuid) -eq $expectedGuid } catch { $false }
    })
    if ($identityMatches.Count -eq 0) {
        Throw-HostLabFailure -Code 'SnapshotAdapterNotFound' -Message "Fresh adapter identity does not contain expected GUID $expectedGuid."
    }
    if ($identityMatches.Count -ne 1) {
        Throw-HostLabFailure -Code 'SnapshotAdapterAmbiguous' -Message "Fresh adapter identity contains multiple matches for GUID $expectedGuid."
    }

    $freshIdentity = $identityMatches[0]
    $index = [int]$freshIdentity.ifIndex
    if ($index -ne [int]$Target.InterfaceIndex) {
        Throw-HostLabFailure -Code 'SnapshotIdentityDrift' -Message 'Expected adapter GUID was rebound to a different InterfaceIndex.'
    }
    $freshMac = ConvertTo-NormalizedMac -Value $freshIdentity.MacAddress
    $expectedMac = ConvertTo-NormalizedMac -Value $Target.MacAddress
    if ([string]::IsNullOrWhiteSpace($freshMac) -or $freshMac -cne $expectedMac) {
        Throw-HostLabFailure -Code 'SnapshotIdentityDrift' -Message 'Expected adapter GUID no longer has the discovered MAC identity.'
    }
    if ([string]$freshIdentity.Name -cne [string]$Target.InterfaceAlias -or
        [string]$freshIdentity.InterfaceDescription -cne [string]$Target.InterfaceDescription) {
        Throw-HostLabFailure -Code 'SnapshotIdentityDrift' -Message 'Expected adapter GUID alias or description changed.'
    }
    if ([string]$freshIdentity.Status -ne 'Up' -or (Test-IsPhysicalAdapter $freshIdentity) -or (Test-IsWifiAdapter $freshIdentity)) {
        Throw-HostLabFailure -Code 'SnapshotAdapterNotUsable' -Message 'Fresh target identity is no longer an Up, virtual, non-Wi-Fi adapter.'
    }
    $addresses = @($Data.IPAddresses | Where-Object { [int]$_.InterfaceIndex -eq $index })
    $routes = @($Data.Routes | Where-Object { [int]$_.InterfaceIndex -eq $index -and (Test-IsDefaultRoute $_) })
    $dns = @($Data.Dns | Where-Object { [int]$_.InterfaceIndex -eq $index })

    $ipv4Addresses = ConvertTo-StableStringArray -Values @($addresses | Where-Object { Test-AddressFamilyValue -Value $_.AddressFamily -Expected 'IPv4' } | ForEach-Object {
        '{0}/{1}' -f $_.IPAddress, $_.PrefixLength
    })
    $ipv6Addresses = ConvertTo-StableStringArray -Values @($addresses | Where-Object { Test-AddressFamilyValue -Value $_.AddressFamily -Expected 'IPv6' } | ForEach-Object {
        '{0}/{1}' -f $_.IPAddress, $_.PrefixLength
    })
    $ipv4Gateways = ConvertTo-StableStringArray -Values @($routes | Where-Object { [string]$_.DestinationPrefix -eq '0.0.0.0/0' } | ForEach-Object { $_.NextHop })
    $ipv6Gateways = ConvertTo-StableStringArray -Values @($routes | Where-Object { [string]$_.DestinationPrefix -eq '::/0' } | ForEach-Object { $_.NextHop })
    $ipv4Dns = ConvertTo-StableStringArray -Values @($dns | Where-Object { Test-AddressFamilyValue -Value $_.AddressFamily -Expected 'IPv4' } | ForEach-Object { @($_.ServerAddresses) })
    $ipv6Dns = ConvertTo-StableStringArray -Values @($dns | Where-Object { Test-AddressFamilyValue -Value $_.AddressFamily -Expected 'IPv6' } | ForEach-Object { @($_.ServerAddresses) })
    $ipv6Routes = ConvertTo-StableStringArray -Values @($Data.Routes | Where-Object {
        [int]$_.InterfaceIndex -eq $index -and ([string]$_.DestinationPrefix).Contains(':')
    } | ForEach-Object {
        '{0}|{1}|{2}|{3}' -f $_.DestinationPrefix, $_.NextHop, (Get-PropertyValue $_ 'RouteMetric' 0), (Get-PropertyValue $_ 'InterfaceMetric' 0)
    })

    $nonTargetIndexes = @($netAdaptersProperty.Value | Where-Object { [int]$_.ifIndex -ne $index } | ForEach-Object { [int]$_.ifIndex })
    $nonTargetAdapters = ConvertTo-StableStringArray -Values @($netAdaptersProperty.Value | Where-Object { [int]$_.ifIndex -ne $index } | ForEach-Object {
        $guid = try { ConvertTo-CanonicalGuid -Value $_.InterfaceGuid } catch { '<invalid-guid>' }
        '{0}|{1}|{2}|{3}|{4}|{5}|{6}' -f $guid, $_.ifIndex, $_.Name, (ConvertTo-NormalizedMac $_.MacAddress), $_.Status, $_.Virtual, $_.HardwareInterface
    })
    $nonTargetAddresses = ConvertTo-StableStringArray -Values @($Data.IPAddresses | Where-Object { $nonTargetIndexes -contains [int]$_.InterfaceIndex } | ForEach-Object {
        '{0}|{1}|{2}/{3}' -f $_.InterfaceIndex, $_.AddressFamily, $_.IPAddress, $_.PrefixLength
    })
    $nonTargetDefaultRoutes = ConvertTo-StableStringArray -Values @($Data.Routes | Where-Object { $nonTargetIndexes -contains [int]$_.InterfaceIndex -and (Test-IsDefaultRoute $_) } | ForEach-Object {
        '{0}|{1}|{2}' -f $_.InterfaceIndex, $_.DestinationPrefix, $_.NextHop
    })
    $nonTargetDefaultRouteMetrics = ConvertTo-StableStringArray -Values @($Data.Routes | Where-Object { $nonTargetIndexes -contains [int]$_.InterfaceIndex -and (Test-IsDefaultRoute $_) } | ForEach-Object {
        '{0}|{1}|{2}|RouteMetric={3}|InterfaceMetric={4}' -f $_.InterfaceIndex, $_.DestinationPrefix, $_.NextHop, (Get-PropertyValue $_ 'RouteMetric' 0), (Get-PropertyValue $_ 'InterfaceMetric' 0)
    })
    $nonTargetDns = ConvertTo-StableStringArray -Values @($Data.Dns | Where-Object { $nonTargetIndexes -contains [int]$_.InterfaceIndex } | ForEach-Object {
        '{0}|{1}|{2}' -f $_.InterfaceIndex, $_.AddressFamily, (@($_.ServerAddresses) -join ',')
    })

    $baselineMatch = $ipv4Addresses.Count -eq 1 -and
        ($ipv4Addresses -contains ('{0}/{1}' -f $script:ExpectedIpv4Address, $script:ExpectedIpv4PrefixLength)) -and
        $ipv4Gateways.Count -eq 0 -and $ipv4Dns.Count -eq 0

    return [pscustomobject][ordered]@{
        capturedAtUtc = [DateTime]::UtcNow.ToString('o')
        interfaceGuid = $expectedGuid
        interfaceIndex = $index
        interfaceAlias = [string]$freshIdentity.Name
        interfaceDescription = [string]$freshIdentity.InterfaceDescription
        macAddress = [string]$freshIdentity.MacAddress
        adapterStatus = [string]$freshIdentity.Status
        ipv4Addresses = $ipv4Addresses
        ipv4DefaultGateways = $ipv4Gateways
        ipv4DnsServerAddresses = $ipv4Dns
        ipv6Addresses = $ipv6Addresses
        ipv6DefaultGateways = $ipv6Gateways
        ipv6DnsServerAddresses = $ipv6Dns
        ipv6Routes = $ipv6Routes
        ownsIpv4DefaultRoute = $ipv4Gateways.Count -gt 0
        ownsIpv6DefaultRoute = $ipv6Gateways.Count -gt 0
        switchRelationshipStatus = [string]$Target.SafetyGate
        baselineMatch = $baselineMatch
        nonTargetState = [pscustomobject][ordered]@{
            adapters = $nonTargetAdapters
            addresses = $nonTargetAddresses
            defaultRoutes = $nonTargetDefaultRoutes
            defaultRouteMetrics = $nonTargetDefaultRouteMetrics
            dns = $nonTargetDns
        }
    }
}

function Get-HostLabLiveSnapshotData {
    try {
        return [pscustomobject]@{
            NetAdapters = @(Get-NetAdapter -IncludeHidden -ErrorAction Stop)
            IPAddresses = @(Get-NetIPAddress -ErrorAction Stop)
            Routes = @(Get-NetRoute -PolicyStore ActiveStore -ErrorAction Stop | Where-Object {
                $_.State -eq 'Alive' -or $null -eq $_.PSObject.Properties['State']
            })
            Dns = @(Get-DnsClientServerAddress -ErrorAction Stop)
        }
    }
    catch {
        Throw-HostLabFailure -Code 'WindowsSnapshotReadFailed' -Message "Independent Windows network snapshot could not be read: $($_.Exception.Message)"
    }
}

function ConvertTo-ComparableTargetSnapshotJson {
    param([Parameter(Mandatory = $true)]$Snapshot)

    $copy = [ordered]@{}
    foreach ($property in $Snapshot.PSObject.Properties) {
        if ($property.Name -notin @('capturedAtUtc', 'baselineMatch', 'nonTargetState')) {
            if ($property.Name -eq 'ipv6DnsServerAddresses' -and
                (Test-Ipv6DnsSemanticallyUnconfigured -Values $property.Value)) {
                $copy[$property.Name] = @()
            }
            else {
                $copy[$property.Name] = $property.Value
            }
        }
    }
    return ($copy | ConvertTo-Json -Depth 8 -Compress)
}

function Test-TargetSnapshotsEqual {
    param(
        [Parameter(Mandatory = $true)]$Before,
        [Parameter(Mandatory = $true)]$After
    )
    return (ConvertTo-ComparableTargetSnapshotJson $Before) -ceq (ConvertTo-ComparableTargetSnapshotJson $After)
}

function Compare-NonTargetSafety {
    param(
        [Parameter(Mandatory = $true)]$Before,
        [Parameter(Mandatory = $true)]$After
    )

    $substantiveDifferences = New-Object System.Collections.Generic.List[string]
    foreach ($field in @('adapters', 'addresses', 'defaultRoutes', 'dns')) {
        if ((@($Before.nonTargetState.$field) -join "`n") -cne (@($After.nonTargetState.$field) -join "`n")) {
            $substantiveDifferences.Add($field)
        }
    }
    $metricDrift = @()
    $beforeMetrics = @($Before.nonTargetState.defaultRouteMetrics)
    $afterMetrics = @($After.nonTargetState.defaultRouteMetrics)
    if (($beforeMetrics -join "`n") -cne ($afterMetrics -join "`n")) {
        $metricDrift = @([pscustomobject][ordered]@{
            observedAtUtc = [string]$After.capturedAtUtc
            before = $beforeMetrics
            after = $afterMetrics
        })
    }
    return [pscustomobject]@{
        SubstantiveEqual = $substantiveDifferences.Count -eq 0
        SubstantiveDifferences = @($substantiveDifferences | ForEach-Object { $_ })
        MetricDrift = $metricDrift
    }
}

function Add-NonTargetMetricDrift {
    param(
        [Parameter(Mandatory = $true)]$Baseline,
        [Parameter(Mandatory = $true)]$Observed,
        [Parameter(Mandatory = $true)]$MutationTracker
    )
    $comparison = Compare-NonTargetSafety -Before $Baseline -After $Observed
    foreach ($drift in @($comparison.MetricDrift)) { $MutationTracker.MetricDrift.Add($drift) }
    return $comparison
}

function Test-HostLabSnapshotsEqual {
    param(
        [Parameter(Mandatory = $true)]$Before,
        [Parameter(Mandatory = $true)]$After
    )

    $nonTarget = Compare-NonTargetSafety -Before $Before -After $After
    return (Test-TargetSnapshotsEqual -Before $Before -After $After) -and $nonTarget.SubstantiveEqual
}

function Clear-IPManEnvironmentVariables {
    param([Parameter(Mandatory = $true)]$EnvironmentVariables)

    $keys = @($EnvironmentVariables.Keys | ForEach-Object { [string]$_ })
    foreach ($key in $keys) {
        if ($key.StartsWith('IPMAN_', [StringComparison]::OrdinalIgnoreCase)) {
            $EnvironmentVariables.Remove($key)
        }
    }
}

function New-DiagnosticProcessStartInfo {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$AdapterGuid
    )

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = 'dotnet'
    $startInfo.Arguments = 'test tests/IPMan.IntegrationTests/IPMan.IntegrationTests.csproj --filter "Category=DnsTruthDiagnostic" --logger "console;verbosity=detailed"'
    $startInfo.WorkingDirectory = $RepoRoot
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    Clear-IPManEnvironmentVariables -EnvironmentVariables $startInfo.EnvironmentVariables
    $startInfo.EnvironmentVariables['IPMAN_DNS_TRUTH_DIAGNOSTIC'] = '1'
    $startInfo.EnvironmentVariables['IPMAN_TEST_ADAPTER_ID'] = $AdapterGuid
    return $startInfo
}

function Invoke-DnsTruthDiagnosticProcess {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$AdapterGuid
    )

    $startInfo = New-DiagnosticProcessStartInfo -RepoRoot $RepoRoot -AdapterGuid $AdapterGuid
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    try {
        if (-not $process.Start()) {
            Throw-HostLabFailure -Code 'DiagnosticProcessStartFailed' -Message 'dotnet diagnostic process did not start.'
        }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $stdout = $stdoutTask.GetAwaiter().GetResult()
        $stderr = $stderrTask.GetAwaiter().GetResult()
        return [pscustomobject]@{
            ExitCode = $process.ExitCode
            Output = ($stdout + [Environment]::NewLine + $stderr).Trim()
        }
    }
    finally {
        Clear-IPManEnvironmentVariables -EnvironmentVariables $startInfo.EnvironmentVariables
        $process.Dispose()
    }
}

function New-DestructiveScenarioProcessStartInfo {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$AdapterGuid,
        [Parameter(Mandatory = $true)]
        [ValidateSet('ManualDnsOne', 'AutomaticDns', 'StaticToStatic')]
        [string]$Scenario,
        [Parameter(Mandatory = $true)][string]$Ipv4Address,
        [Parameter(Mandatory = $true)][string]$DnsAddress,
        [Parameter(Mandatory = $true)][string]$EvidenceRoot
    )

    $startInfo = New-Object System.Diagnostics.ProcessStartInfo
    $startInfo.FileName = 'dotnet'
    $startInfo.Arguments = 'test tests/IPMan.IntegrationTests/IPMan.IntegrationTests.csproj --filter "Category=DestructiveNetwork" --logger "console;verbosity=detailed"'
    $startInfo.WorkingDirectory = $RepoRoot
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    Clear-IPManEnvironmentVariables -EnvironmentVariables $startInfo.EnvironmentVariables
    $startInfo.EnvironmentVariables['IPMAN_DESTRUCTIVE_NETWORK_TESTS'] = '1'
    $startInfo.EnvironmentVariables['IPMAN_TEST_ADAPTER_ID'] = $AdapterGuid
    $startInfo.EnvironmentVariables['IPMAN_TEST_ADAPTER_IS_ISOLATED'] = 'YES_DISPOSABLE_ISOLATED_ADAPTER'
    $startInfo.EnvironmentVariables['IPMAN_DESTRUCTIVE_FILTER_ACK'] = 'DESTRUCTIVE_NETWORK_FILTER_APPLIED'
    $startInfo.EnvironmentVariables['IPMAN_TEST_CONFLICT_RISK_ACK'] = 'YES_ACCEPT_IP_CONFLICT_RISK'
    $startInfo.EnvironmentVariables['IPMAN_TEST_SCENARIO'] = $Scenario
    $startInfo.EnvironmentVariables['IPMAN_TEST_IPV4'] = $Ipv4Address
    $startInfo.EnvironmentVariables['IPMAN_TEST_SUBNET_MASK'] = '255.255.255.0'
    $startInfo.EnvironmentVariables['IPMAN_TEST_GATEWAY'] = 'NONE'
    $startInfo.EnvironmentVariables['IPMAN_TEST_DNS'] = $DnsAddress
    $startInfo.EnvironmentVariables['IPMAN_TEST_EVIDENCE_ROOT'] = $EvidenceRoot
    return $startInfo
}

function Invoke-DestructiveScenarioProcess {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$AdapterGuid,
        [Parameter(Mandatory = $true)]
        [ValidateSet('ManualDnsOne', 'AutomaticDns', 'StaticToStatic')]
        [string]$Scenario,
        [Parameter(Mandatory = $true)][string]$Ipv4Address,
        [Parameter(Mandatory = $true)][string]$DnsAddress,
        [Parameter(Mandatory = $true)][string]$EvidenceRoot
    )

    $startInfo = New-DestructiveScenarioProcessStartInfo -RepoRoot $RepoRoot -AdapterGuid $AdapterGuid -Scenario $Scenario -Ipv4Address $Ipv4Address -DnsAddress $DnsAddress -EvidenceRoot $EvidenceRoot
    $process = New-Object System.Diagnostics.Process
    $process.StartInfo = $startInfo
    $processStarted = $false
    try {
        if (-not $process.Start()) {
            Throw-HostLabFailure -Code 'MutationProcessStartFailed' -Message 'Controlled production-harness process did not start.'
        }
        $processStarted = $true
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $process.WaitForExit()
        $stdout = $stdoutTask.GetAwaiter().GetResult()
        $stderr = $stderrTask.GetAwaiter().GetResult()
        return [pscustomobject]@{
            ExitCode = $process.ExitCode
            Stdout = $stdout.TrimEnd()
            Stderr = $stderr.TrimEnd()
            Output = ($stdout + [Environment]::NewLine + $stderr).Trim()
            Scenario = $Scenario
            ProcessStarted = $true
        }
    }
    catch {
        if ($processStarted) {
            $_.Exception.Data['MutationProcessStarted'] = $true
        }
        throw
    }
    finally {
        Clear-IPManEnvironmentVariables -EnvironmentVariables $startInfo.EnvironmentVariables
        $process.Dispose()
    }
}

function Test-DestructiveScenarioExecutionProof {
    param(
        [Parameter(Mandatory = $true)]$ProcessResult,
        [Parameter(Mandatory = $true)][string]$AdapterGuid,
        [Parameter(Mandatory = $true)][string]$Scenario
    )

    if ([int]$ProcessResult.ExitCode -ne 0) {
        Throw-HostLabFailure -Code 'MutationHarnessFailed' -Message "Controlled $Scenario scenario exited with code $($ProcessResult.ExitCode)."
    }
    $output = [string]$ProcessResult.Output
    foreach ($marker in @('DESTRUCTIVE NETWORK TEST ENABLED FOR ONE EXPLICIT SCENARIO.', "Scenario: $Scenario")) {
        if ($output.IndexOf($marker, [StringComparison]::Ordinal) -lt 0) {
            Throw-HostLabFailure -Code 'MutationExecutionNotProven' -Message "Controlled scenario marker missing: $marker"
        }
    }
    $canonicalGuid = ConvertTo-CanonicalGuid -Value $AdapterGuid
    if ($output.IndexOf($canonicalGuid, [StringComparison]::OrdinalIgnoreCase) -lt 0) {
        Throw-HostLabFailure -Code 'MutationGuidMismatch' -Message 'Controlled scenario output did not contain the exact target GUID.'
    }
    if ($output -match '(?i)No test matches|No test is available|NOT EXECUTED|Skipped:\s*[1-9]|Tests skipped:\s*[1-9]') {
        Throw-HostLabFailure -Code 'MutationScenarioSkipped' -Message 'Controlled scenario was skipped, unmatched, or refused.'
    }
    return $true
}

function Assert-ControlledMutationSafety {
    param(
        [Parameter(Mandatory = $true)]$Baseline,
        [Parameter(Mandatory = $true)]$Observed
    )

    $identityFields = @('interfaceGuid', 'interfaceIndex', 'interfaceAlias', 'interfaceDescription', 'macAddress', 'adapterStatus')
    foreach ($field in $identityFields) {
        if ([string]$Baseline.$field -cne [string]$Observed.$field) {
            Throw-HostLabFailure -Code 'SnapshotIdentityDrift' -Message "Controlled validation identity field changed: $field"
        }
    }
    if (@($Observed.ipv4DefaultGateways).Count -ne 0 -or @($Observed.ipv6DefaultGateways).Count -ne 0 -or $Observed.ownsIpv4DefaultRoute -or $Observed.ownsIpv6DefaultRoute) {
        Throw-HostLabFailure -Code 'DefaultRouteCreated' -Message 'Controlled validation observed an unexpected target default route or gateway.'
    }
    foreach ($field in @('ipv6Addresses', 'ipv6Routes')) {
        if ((@($Baseline.$field) -join '|') -cne (@($Observed.$field) -join '|')) {
            Throw-HostLabFailure -Code 'MutationVerificationFailed' -Message "Controlled validation changed forbidden IPv6 state: $field"
        }
    }
    if (-not (Test-Ipv6DnsServerAddressesEqual -Before $Baseline.ipv6DnsServerAddresses -After $Observed.ipv6DnsServerAddresses)) {
        Throw-HostLabFailure -Code 'MutationVerificationFailed' -Message 'Controlled validation changed forbidden IPv6 state: ipv6DnsServerAddresses'
    }
    return $true
}

function Assert-ControlledExpectedMutationSnapshot {
    param(
        [Parameter(Mandatory = $true)]$Observed,
        [Parameter(Mandatory = $true)][string]$ExpectedIpv4,
        [string[]]$ExpectedIpv4Dns = @()
    )

    if ((@($Observed.ipv4Addresses) -join '|') -cne "$ExpectedIpv4/$script:ExpectedIpv4PrefixLength") {
        Throw-HostLabFailure -Code 'MutationVerificationFailed' -Message 'Observed IPv4 address did not exactly match the controlled expectation.'
    }
    if ((@($Observed.ipv4DnsServerAddresses) -join '|') -cne (@($ExpectedIpv4Dns) -join '|')) {
        Throw-HostLabFailure -Code 'MutationVerificationFailed' -Message 'Observed IPv4 DNS did not exactly match the controlled expectation.'
    }
    return $true
}

function Test-DiagnosticExecutionProof {
    param(
        [Parameter(Mandatory = $true)]$ProcessResult,
        [Parameter(Mandatory = $true)][string]$AdapterGuid
    )

    if ([int]$ProcessResult.ExitCode -ne 0) {
        Throw-HostLabFailure -Code 'DiagnosticFailed' -Message "DNS truth diagnostic exited with code $($ProcessResult.ExitCode)."
    }

    $output = [string]$ProcessResult.Output
    foreach ($marker in $script:DiagnosticMarkers) {
        if ($output.IndexOf($marker, [StringComparison]::Ordinal) -lt 0) {
            Throw-HostLabFailure -Code 'DiagnosticExecutionNotProven' -Message "Diagnostic output marker missing: $marker"
        }
    }

    $canonicalGuid = ConvertTo-CanonicalGuid -Value $AdapterGuid
    $guidPattern = '(?im)^\s*ExactAdapterId:\s*\{?([0-9A-F]{8}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{4}-[0-9A-F]{12})\}?\s*$'
    $guidMatch = [regex]::Match($output, $guidPattern)
    if (-not $guidMatch.Success -or -not $guidMatch.Groups[1].Value.Equals($canonicalGuid, [StringComparison]::OrdinalIgnoreCase)) {
        Throw-HostLabFailure -Code 'DiagnosticGuidMismatch' -Message 'Diagnostic output did not contain the exact canonical target GUID.'
    }
    if ($output -match '(?i)No test matches|No test is available|Skipped:\s*[1-9]|Tests skipped:\s*[1-9]') {
        Throw-HostLabFailure -Code 'DiagnosticSkipped' -Message 'Diagnostic output indicates a skipped or unmatched test.'
    }

    return $true
}

function Get-RepositoryProvenance {
    param([Parameter(Mandatory = $true)][string]$RepoRoot)

    if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot '.git'))) {
        Throw-HostLabFailure -Code 'RepositoryNotFound' -Message 'Repository .git directory was not found.'
    }
    $fullSha = (& git -C $RepoRoot rev-parse HEAD 2>&1 | Out-String).Trim()
    if ($LASTEXITCODE -ne 0 -or $fullSha -notmatch '^[0-9a-fA-F]{40}$') {
        Throw-HostLabFailure -Code 'GitMetadataFailed' -Message 'Could not resolve repository HEAD.'
    }
    $status = @(& git -C $RepoRoot status --short --untracked-files=no 2>&1)
    if ($LASTEXITCODE -ne 0) {
        Throw-HostLabFailure -Code 'GitMetadataFailed' -Message 'Could not read tracked working-tree status.'
    }
    return [pscustomobject]@{
        fullSha = $fullSha
        shortSha = $fullSha.Substring(0, 12)
        trackedWorkingTreeClean = @($status).Count -eq 0
        trackedStatus = @($status)
    }
}

function New-EvidenceDirectoryPath {
    param(
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$Action,
        [string]$RunId = ([Guid]::NewGuid().ToString('N')),
        [DateTime]$UtcNow = [DateTime]::UtcNow
    )

    $stamp = $UtcNow.ToString('yyyyMMddTHHmmssfffZ')
    return Join-Path $RepoRoot ("artifacts/host-lab/{0}-{1}-{2}" -f $stamp, $Action.ToLowerInvariant(), $RunId)
}

function Write-JsonFile {
    param([string]$Path, $Value)
    $Value | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $Path -Encoding UTF8
}

function Write-HostLabEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$Directory,
        [Parameter(Mandatory = $true)]$Summary,
        $Before,
        $After,
        [string]$DiagnosticOutput = '',
        [string[]]$Transcript = @()
    )

    New-Item -ItemType Directory -Path $Directory -Force | Out-Null
    Write-JsonFile -Path (Join-Path $Directory 'summary.json') -Value $Summary
    Write-JsonFile -Path (Join-Path $Directory 'before.json') -Value $Before
    Write-JsonFile -Path (Join-Path $Directory 'after.json') -Value $After
    Set-Content -LiteralPath (Join-Path $Directory 'dns-truth-diagnostic.txt') -Value $DiagnosticOutput -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $Directory 'transcript.txt') -Value $Transcript -Encoding UTF8
}

function Write-ControlledStateEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$Directory,
        [Parameter(Mandatory = $true)]
        [ValidateSet('original-before', 'dns-mutated', 'dns-rollback', 'ipv4-mutated', 'ipv4-rollback', 'non-target-drift')]
        [string]$Name,
        [Parameter(Mandatory = $true)]$Value
    )
    New-Item -ItemType Directory -Path $Directory -Force | Out-Null
    Write-JsonFile -Path (Join-Path $Directory "$Name.json") -Value $Value
}

function Write-ControlledProcessEvidence {
    param(
        [Parameter(Mandatory = $true)][string]$Directory,
        [Parameter(Mandatory = $true)]
        [ValidateSet('dns-mutation', 'dns-rollback', 'ipv4-mutation', 'ipv4-rollback')]
        [string]$Name,
        [string]$Value = ''
    )
    New-Item -ItemType Directory -Path $Directory -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $Directory "$Name.txt") -Value $Value -Encoding UTF8
}

function Format-ControlledProcessEvidence {
    param(
        $ProcessResult,
        $ProcessError,
        [Parameter(Mandatory = $true)][string]$Scenario
    )

    $lines = New-Object System.Collections.Generic.List[string]
    $processStarted = $false
    $exitCode = '<unavailable>'
    $stdout = $null
    $stderr = $null
    $combinedOutput = ''
    if ($null -ne $ProcessResult) {
        $processStarted = [bool](Get-PropertyValue -InputObject $ProcessResult -Name 'ProcessStarted' -DefaultValue $false)
        $exitCode = Get-PropertyValue -InputObject $ProcessResult -Name 'ExitCode' -DefaultValue '<unavailable>'
        $stdout = Get-PropertyValue -InputObject $ProcessResult -Name 'Stdout' -DefaultValue $null
        $stderr = Get-PropertyValue -InputObject $ProcessResult -Name 'Stderr' -DefaultValue $null
        $combinedOutput = [string](Get-PropertyValue -InputObject $ProcessResult -Name 'Output' -DefaultValue '')
    }
    $lines.Add("Scenario: $Scenario")
    $lines.Add("ProcessStarted: $processStarted")
    $lines.Add("ExitCode: $exitCode")
    $lines.Add("ProcessException: $(if ($null -ne $ProcessError) { [string]$ProcessError } else { '<none>' })")

    if ($null -eq $stdout -and $null -eq $stderr) {
        $lines.Add('--- COMBINED OUTPUT ---')
        $lines.Add($combinedOutput)
    }
    else {
        $lines.Add('--- STDOUT ---')
        $lines.Add([string]$stdout)
        $lines.Add('--- STDERR ---')
        $lines.Add([string]$stderr)
    }
    return $lines -join [Environment]::NewLine
}

function Invoke-ControlledScenarioAttempt {
    param(
        [Parameter(Mandatory = $true)]$Target,
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$EvidenceDirectory,
        [Parameter(Mandatory = $true)][hashtable]$Operations,
        [Parameter(Mandatory = $true)][string]$Scenario,
        [Parameter(Mandatory = $true)][string]$Ipv4Address,
        [Parameter(Mandatory = $true)][string]$DnsAddress,
        [Parameter(Mandatory = $true)][string]$StateName,
        [Parameter(Mandatory = $true)][string]$ProcessName,
        [Parameter(Mandatory = $true)]$Baseline,
        [Parameter(Mandatory = $true)]$MutationTracker
    )

    $processResult = $null
    $processError = $null
    $snapshot = $null
    $snapshotError = $null
    try {
        $processResult = & $Operations.RunScenario $RepoRoot $Target.InterfaceGuid $Scenario $Ipv4Address $DnsAddress (Join-Path $EvidenceDirectory 'production-harness')
        if ([bool](Get-PropertyValue -InputObject $processResult -Name 'ProcessStarted' -DefaultValue $false)) {
            $MutationTracker.Attempted = $true
        }
    }
    catch {
        $processError = $_
        if ($_.Exception.Data.Contains('MutationProcessStarted') -and [bool]$_.Exception.Data['MutationProcessStarted']) {
            $MutationTracker.Attempted = $true
        }
    }
    $processEvidence = Format-ControlledProcessEvidence -ProcessResult $processResult -ProcessError $processError -Scenario $Scenario
    & $Operations.SaveProcess $EvidenceDirectory $ProcessName $processEvidence
    try {
        $snapshot = Get-HostLabSnapshot -Target $Target -Data (& $Operations.GetSnapshotData)
        & $Operations.SaveState $EvidenceDirectory $StateName $snapshot
        if (-not (Test-TargetSnapshotsEqual -Before $Baseline -After $snapshot)) {
            $MutationTracker.Performed = $true
        }
    }
    catch {
        $snapshotError = $_
    }
    return [pscustomobject]@{
        ProcessResult = $processResult
        ProcessError = $processError
        Snapshot = $snapshot
        SnapshotError = $snapshotError
    }
}

function Assert-ControlledApplyAttempt {
    param(
        [Parameter(Mandatory = $true)]$Attempt,
        [Parameter(Mandatory = $true)]$Baseline,
        [Parameter(Mandatory = $true)][string]$AdapterGuid,
        [Parameter(Mandatory = $true)][string]$Scenario,
        [Parameter(Mandatory = $true)][string]$ExpectedIpv4,
        [string[]]$ExpectedDns = @()
    )
    if ($null -ne $Attempt.SnapshotError) { throw $Attempt.SnapshotError.Exception }
    if ($null -eq $Attempt.Snapshot) {
        Throw-HostLabFailure -Code 'MutationVerificationFailed' -Message 'Controlled mutation produced no independent snapshot.'
    }
    Assert-ControlledMutationSafety -Baseline $Baseline -Observed $Attempt.Snapshot | Out-Null
    if ($null -ne $Attempt.ProcessError) { throw $Attempt.ProcessError.Exception }
    Test-DestructiveScenarioExecutionProof -ProcessResult $Attempt.ProcessResult -AdapterGuid $AdapterGuid -Scenario $Scenario | Out-Null
    Assert-ControlledExpectedMutationSnapshot -Observed $Attempt.Snapshot -ExpectedIpv4 $ExpectedIpv4 -ExpectedIpv4Dns $ExpectedDns | Out-Null
}

function Test-ControlledRollback {
    param(
        [Parameter(Mandatory = $true)]$Attempt,
        [Parameter(Mandatory = $true)]$Baseline
    )
    if ($null -ne $Attempt.SnapshotError -or $null -eq $Attempt.Snapshot) { return $false }
    return Test-TargetSnapshotsEqual -Before $Baseline -After $Attempt.Snapshot
}

function Invoke-ControlledStage {
    param(
        [Parameter(Mandatory = $true)]$Target,
        [Parameter(Mandatory = $true)]$Baseline,
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$EvidenceDirectory,
        [Parameter(Mandatory = $true)][hashtable]$Operations,
        [Parameter(Mandatory = $true)][string]$ApplyScenario,
        [Parameter(Mandatory = $true)][string]$ApplyIpv4,
        [Parameter(Mandatory = $true)][string]$ApplyDns,
        [Parameter(Mandatory = $true)][string]$ApplyStateName,
        [Parameter(Mandatory = $true)][string]$ApplyProcessName,
        [Parameter(Mandatory = $true)][string]$RollbackScenario,
        [Parameter(Mandatory = $true)][string]$RollbackIpv4,
        [Parameter(Mandatory = $true)][string]$RollbackDns,
        [Parameter(Mandatory = $true)][string]$RollbackStateName,
        [Parameter(Mandatory = $true)][string]$RollbackProcessName,
        [Parameter(Mandatory = $true)][string]$ExpectedIpv4,
        [string[]]$ExpectedDns = @(),
        [Parameter(Mandatory = $true)]$MutationTracker
    )

    $apply = Invoke-ControlledScenarioAttempt -Target $Target -RepoRoot $RepoRoot -EvidenceDirectory $EvidenceDirectory -Operations $Operations -Scenario $ApplyScenario -Ipv4Address $ApplyIpv4 -DnsAddress $ApplyDns -StateName $ApplyStateName -ProcessName $ApplyProcessName -Baseline $Baseline -MutationTracker $MutationTracker
    $primaryError = $null
    try {
        Assert-ControlledApplyAttempt -Attempt $apply -Baseline $Baseline -AdapterGuid $Target.InterfaceGuid -Scenario $ApplyScenario -ExpectedIpv4 $ExpectedIpv4 -ExpectedDns $ExpectedDns
    }
    catch { $primaryError = $_ }

    if ($null -ne $apply.Snapshot) {
        $applyNonTarget = Add-NonTargetMetricDrift -Baseline $Baseline -Observed $apply.Snapshot -MutationTracker $MutationTracker
        if (-not $applyNonTarget.SubstantiveEqual) {
            $primaryError = [pscustomobject]@{
                Exception = (New-HostLabException -Code 'NON_TARGET_STATE_CHANGED' -Message ('Substantive non-target state changed: ' + ($applyNonTarget.SubstantiveDifferences -join ',')))
            }
        }
    }

    $targetNeedsRollback = $null -eq $apply.Snapshot -or $null -ne $apply.SnapshotError -or -not (Test-TargetSnapshotsEqual -Before $Baseline -After $apply.Snapshot)
    if ($targetNeedsRollback) {
        $rollback = Invoke-ControlledScenarioAttempt -Target $Target -RepoRoot $RepoRoot -EvidenceDirectory $EvidenceDirectory -Operations $Operations -Scenario $RollbackScenario -Ipv4Address $RollbackIpv4 -DnsAddress $RollbackDns -StateName $RollbackStateName -ProcessName $RollbackProcessName -Baseline $Baseline -MutationTracker $MutationTracker
    }
    else {
        $rollback = [pscustomobject]@{ ProcessResult = $null; ProcessError = $null; Snapshot = $apply.Snapshot; SnapshotError = $null; RollbackSkipped = $true }
        & $Operations.SaveState $EvidenceDirectory $RollbackStateName $apply.Snapshot
        & $Operations.SaveProcess $EvidenceDirectory $RollbackProcessName 'SKIPPED — target already exactly matched the original baseline.'
    }

    if (-not (Test-ControlledRollback -Attempt $rollback -Baseline $Baseline)) {
        Throw-HostLabFailure -Code 'ROLLBACK_FAILED' -Message "$ApplyScenario rollback did not restore target equality."
    }
    $finalNonTarget = Add-NonTargetMetricDrift -Baseline $Baseline -Observed $rollback.Snapshot -MutationTracker $MutationTracker
    & $Operations.SaveState $EvidenceDirectory 'non-target-drift' @($MutationTracker.MetricDrift | ForEach-Object { $_ })
    if (-not $finalNonTarget.SubstantiveEqual) {
        Throw-HostLabFailure -Code 'NON_TARGET_STATE_CHANGED' -Message ('Substantive non-target state changed: ' + ($finalNonTarget.SubstantiveDifferences -join ', '))
    }
    if ($null -ne $primaryError) { throw $primaryError.Exception }

    return [pscustomobject]@{
        Mutation = $apply.Snapshot
        Rollback = $rollback.Snapshot
        RollbackSkipped = [bool](Get-PropertyValue -InputObject $rollback -Name 'RollbackSkipped' -DefaultValue $false)
    }
}

function Invoke-ControlledValidation {
    param(
        [Parameter(Mandatory = $true)]$Target,
        [Parameter(Mandatory = $true)]$Before,
        [Parameter(Mandatory = $true)][string]$RepoRoot,
        [Parameter(Mandatory = $true)][string]$EvidenceDirectory,
        [Parameter(Mandatory = $true)][hashtable]$Operations,
        [Parameter(Mandatory = $true)]$MutationTracker
    )

    & $Operations.SaveState $EvidenceDirectory 'original-before' $Before
    $MutationTracker.Allowed = $true
    $dns = Invoke-ControlledStage -Target $Target -Baseline $Before -RepoRoot $RepoRoot -EvidenceDirectory $EvidenceDirectory -Operations $Operations -ApplyScenario 'ManualDnsOne' -ApplyIpv4 $script:ExpectedIpv4Address -ApplyDns $script:ValidationDnsAddress -ApplyStateName 'dns-mutated' -ApplyProcessName 'dns-mutation' -RollbackScenario 'AutomaticDns' -RollbackIpv4 $script:ExpectedIpv4Address -RollbackDns 'NONE' -RollbackStateName 'dns-rollback' -RollbackProcessName 'dns-rollback' -ExpectedIpv4 $script:ExpectedIpv4Address -ExpectedDns @($script:ValidationDnsAddress) -MutationTracker $MutationTracker
    $ipv4 = Invoke-ControlledStage -Target $Target -Baseline $Before -RepoRoot $RepoRoot -EvidenceDirectory $EvidenceDirectory -Operations $Operations -ApplyScenario 'StaticToStatic' -ApplyIpv4 $script:ValidationIpv4Address -ApplyDns 'NONE' -ApplyStateName 'ipv4-mutated' -ApplyProcessName 'ipv4-mutation' -RollbackScenario 'StaticToStatic' -RollbackIpv4 $script:ExpectedIpv4Address -RollbackDns 'NONE' -RollbackStateName 'ipv4-rollback' -RollbackProcessName 'ipv4-rollback' -ExpectedIpv4 $script:ValidationIpv4Address -MutationTracker $MutationTracker

    return [pscustomobject]@{
        DnsMutation = $dns.Mutation
        DnsRollback = $dns.Rollback
        Ipv4Mutation = $ipv4.Mutation
        Ipv4Rollback = $ipv4.Rollback
        FinalSnapshot = $ipv4.Rollback
        FinalEqualsBefore = Test-HostLabSnapshotsEqual -Before $Before -After $ipv4.Rollback
    }
}

function Assert-HostLabPrerequisites {
    param([Parameter(Mandatory = $true)][string]$RepoRoot)

    $requiredCommands = @('Get-NetAdapter', 'Get-NetIPAddress', 'Get-NetRoute', 'Get-DnsClientServerAddress', 'dotnet', 'git')
    $missing = @($requiredCommands | Where-Object { $null -eq (Get-Command $_ -ErrorAction SilentlyContinue) })
    if ($missing.Count -gt 0) {
        Throw-HostLabFailure -Code 'PrerequisiteMissing' -Message ('Missing required read-only commands: ' + ($missing -join ', '))
    }
    if (-not (Test-Path -LiteralPath (Join-Path $RepoRoot 'tests/IPMan.IntegrationTests/IPMan.IntegrationTests.csproj'))) {
        Throw-HostLabFailure -Code 'RepositoryNotFound' -Message 'DNS truth diagnostic project was not found.'
    }
}

function Test-CurrentProcessElevated {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function New-ProductionHostLabOperations {
    return @{
        AssertPrerequisites = { param($Root) Assert-HostLabPrerequisites -RepoRoot $Root }
        GetProvenance = { param($Root) Get-RepositoryProvenance -RepoRoot $Root }
        GetDiscoveryData = { param($Name) Get-HostLabLiveData -SwitchName $Name }
        GetSnapshotData = { Get-HostLabLiveSnapshotData }
        IsElevated = { Test-CurrentProcessElevated }
        RunDiagnostic = { param($Root, $Guid) Invoke-DnsTruthDiagnosticProcess -RepoRoot $Root -AdapterGuid $Guid }
        RunScenario = {
            param($Root, $Guid, $Scenario, $Ipv4, $Dns, $EvidenceRoot)
            Invoke-DestructiveScenarioProcess -RepoRoot $Root -AdapterGuid $Guid -Scenario $Scenario -Ipv4Address $Ipv4 -DnsAddress $Dns -EvidenceRoot $EvidenceRoot
        }
        SaveState = { param($Directory, $Name, $Value) Write-ControlledStateEvidence -Directory $Directory -Name $Name -Value $Value }
        SaveProcess = { param($Directory, $Name, $Value) Write-ControlledProcessEvidence -Directory $Directory -Name $Name -Value $Value }
        WriteEvidence = {
            param($Directory, $Summary, $Before, $After, $DiagnosticOutput, $Transcript)
            Write-HostLabEvidence -Directory $Directory -Summary $Summary -Before $Before -After $After -DiagnosticOutput $DiagnosticOutput -Transcript $Transcript
        }
    }
}

function Invoke-HostLabOrchestration {
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('Preflight', 'DiscoverTarget', 'DnsTruthDiagnostic', 'CollectEvidence', 'ControlledValidation')]
        [string]$Action,
        [string]$ExpectedAdapterGuid,
        [string]$SwitchName = $script:DefaultSwitchName,
        [string]$AliasHint = $script:DefaultAliasHint,
        [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path,
        [Parameter(Mandatory = $true)][hashtable]$Operations
    )

    $started = [DateTime]::UtcNow
    $runId = [Guid]::NewGuid().ToString('N')
    $evidenceDirectory = New-EvidenceDirectoryPath -RepoRoot $RepoRoot -Action $Action -RunId $runId -UtcNow $started
    $before = $null
    $after = $null
    $target = $null
    $provenance = $null
    $diagnosticResult = $null
    $diagnosticProven = $false
    $beforeAfterEqual = $null
    $controlledResult = $null
    $mutationTracker = [pscustomobject]@{
        Allowed = $false
        Attempted = $false
        Performed = $false
        MetricDrift = (New-Object System.Collections.Generic.List[object])
    }
    $transcript = New-Object System.Collections.Generic.List[string]
    $transcript.Add("StartedAtUtc: $($started.ToString('o'))")
    $transcript.Add("Action: $Action")

    try {
        & $Operations.AssertPrerequisites $RepoRoot
        $provenance = & $Operations.GetProvenance $RepoRoot
        if ($Action -eq 'ControlledValidation' -and [string]::IsNullOrWhiteSpace($ExpectedAdapterGuid)) {
            Throw-HostLabFailure -Code 'ExpectedGuidRequiredForMutation' -Message 'Controlled validation requires the exact GUID returned by a completed direct discovery.'
        }
        if ($Action -ne 'Preflight') {
            $discoveryData = & $Operations.GetDiscoveryData $SwitchName
            $target = Resolve-HostLabTarget -Data $discoveryData -SwitchName $SwitchName -AliasHint $AliasHint -ExpectedAdapterGuid $ExpectedAdapterGuid
            $before = Get-HostLabSnapshot -Target $target -Data (& $Operations.GetSnapshotData)
            if (-not $before.baselineMatch) {
                Throw-HostLabFailure -Code 'BaselineMismatch' -Message 'Target IPv4, gateway, or IPv4 DNS does not match the approved isolated baseline.'
            }
            if ($Action -eq 'ControlledValidation') {
                if (-not (& $Operations.IsElevated)) {
                    Throw-HostLabFailure -Code 'ELEVATION_REQUIRED_FOR_MUTATION' -Message 'Controlled validation requires a manually elevated Windows process.'
                }
                $controlledResult = Invoke-ControlledValidation -Target $target -Before $before -RepoRoot $RepoRoot -EvidenceDirectory $evidenceDirectory -Operations $Operations -MutationTracker $mutationTracker
                $after = $controlledResult.FinalSnapshot
                $beforeAfterEqual = $controlledResult.FinalEqualsBefore
            }
            elseif ($Action -eq 'DnsTruthDiagnostic') {
                $diagnosticError = $null
                try {
                    $diagnosticResult = & $Operations.RunDiagnostic $RepoRoot $target.InterfaceGuid
                }
                catch {
                    $diagnosticError = $_
                }

                $afterSnapshotError = $null
                try {
                    $after = Get-HostLabSnapshot -Target $target -Data (& $Operations.GetSnapshotData)
                }
                catch {
                    $afterSnapshotError = $_
                }

                if ($null -ne $afterSnapshotError) {
                    if ($afterSnapshotError.Exception.Data.Contains('HostLabFailureCode')) {
                        throw $afterSnapshotError.Exception
                    }
                    Throw-HostLabFailure -Code 'AfterSnapshotReadFailed' -Message "AFTER snapshot failed after diagnostic execution: $($afterSnapshotError.Exception.Message)"
                }

                $beforeAfterEqual = Test-HostLabSnapshotsEqual -Before $before -After $after
                Add-NonTargetMetricDrift -Baseline $before -Observed $after -MutationTracker $mutationTracker | Out-Null
                if (-not $beforeAfterEqual) {
                    Throw-HostLabFailure -Code 'ReadOnlyDiagnosticStateChanged' -Message 'Independent target snapshots differ.'
                }

                if ($null -ne $diagnosticError) {
                    if ($diagnosticError.Exception.Data.Contains('HostLabFailureCode')) {
                        throw $diagnosticError.Exception
                    }
                    Throw-HostLabFailure -Code 'DiagnosticProcessFailed' -Message "DNS truth diagnostic process failed: $($diagnosticError.Exception.Message)"
                }
                $diagnosticProven = Test-DiagnosticExecutionProof -ProcessResult $diagnosticResult -AdapterGuid $target.InterfaceGuid
            }
            else {
                $after = Get-HostLabSnapshot -Target $target -Data (& $Operations.GetSnapshotData)
                $beforeAfterEqual = Test-HostLabSnapshotsEqual -Before $before -After $after
                Add-NonTargetMetricDrift -Baseline $before -Observed $after -MutationTracker $mutationTracker | Out-Null
                if (-not $beforeAfterEqual) {
                    Throw-HostLabFailure -Code 'ReadOnlyDiagnosticStateChanged' -Message 'Independent target snapshots differ.'
                }
            }
        }

        $completed = [DateTime]::UtcNow
        $summary = [pscustomobject][ordered]@{
            schemaVersion = $script:HostLabSchemaVersion
            runId = $runId
            action = $Action
            startedAtUtc = $started.ToString('o')
            completedAtUtc = $completed.ToString('o')
            success = $true
            failureCode = $null
            hostOsVersion = [Environment]::OSVersion.VersionString
            repoCommit = if ($provenance) { $provenance.fullSha } else { $null }
            repoShortCommit = if ($provenance) { $provenance.shortSha } else { $null }
            trackedWorkingTreeClean = if ($provenance) { $provenance.trackedWorkingTreeClean } else { $null }
            targetInterfaceGuid = if ($target) { $target.InterfaceGuid } else { $null }
            interfaceIndex = if ($target) { $target.InterfaceIndex } else { $null }
            switchName = if ($target) { $target.SwitchName } else { $SwitchName }
            switchType = if ($target) { $target.SwitchType } else { $null }
            managementDefaultRouteInterfaces = if ($target) { $target.ManagementInterfaces } else { @() }
            targetOwnsIpv4DefaultRoute = if ($target) { $target.OwnsIpv4DefaultRoute } else { $null }
            targetOwnsIpv6DefaultRoute = if ($target) { $target.OwnsIpv6DefaultRoute } else { $null }
            targetIsWifi = if ($target) { $target.IsWifi } else { $null }
            targetIsPhysical = if ($target) { $target.IsPhysical } else { $null }
            baselineMatch = if ($before) { $before.baselineMatch } else { $null }
            diagnosticExitCode = if ($diagnosticResult) { $diagnosticResult.ExitCode } else { $null }
            diagnosticExecutionProven = $diagnosticProven
            beforeAfterEqual = $beforeAfterEqual
            controlledDnsMutationVerified = if ($controlledResult) { $true } else { $null }
            controlledDnsRollbackVerified = if ($controlledResult) { $true } else { $null }
            controlledIpv4MutationVerified = if ($controlledResult) { $true } else { $null }
            controlledIpv4RollbackVerified = if ($controlledResult) { $true } else { $null }
            nonTargetMetricDrift = @($mutationTracker.MetricDrift | ForEach-Object { $_ })
            mutationAllowed = $mutationTracker.Allowed
            mutationAttempted = $mutationTracker.Attempted
            mutationPerformed = $mutationTracker.Performed
        }
        $transcript.Add("CompletedAtUtc: $($completed.ToString('o'))")
        $transcript.Add('Success: True')
        & $Operations.WriteEvidence $evidenceDirectory $summary $before $after $(if ($diagnosticResult) { $diagnosticResult.Output } else { '' }) $transcript
        return [pscustomobject]@{ Success = $true; FailureCode = $null; Target = $target; Summary = $summary; EvidenceDirectory = $evidenceDirectory }
    }
    catch {
        $completed = [DateTime]::UtcNow
        $failureCode = if ($_.Exception.Data.Contains('HostLabFailureCode')) { [string]$_.Exception.Data['HostLabFailureCode'] } else { 'UnhandledReadFailure' }
        $summary = [pscustomobject][ordered]@{
            schemaVersion = $script:HostLabSchemaVersion
            runId = $runId
            action = $Action
            startedAtUtc = $started.ToString('o')
            completedAtUtc = $completed.ToString('o')
            success = $false
            failureCode = $failureCode
            failureMessage = $_.Exception.Message
            hostOsVersion = [Environment]::OSVersion.VersionString
            repoCommit = if ($provenance) { $provenance.fullSha } else { $null }
            repoShortCommit = if ($provenance) { $provenance.shortSha } else { $null }
            trackedWorkingTreeClean = if ($provenance) { $provenance.trackedWorkingTreeClean } else { $null }
            targetInterfaceGuid = if ($target) { $target.InterfaceGuid } else { $null }
            interfaceIndex = if ($target) { $target.InterfaceIndex } else { $null }
            switchName = if ($target) { $target.SwitchName } else { $SwitchName }
            switchType = if ($target) { $target.SwitchType } else { $null }
            managementDefaultRouteInterfaces = if ($target) { $target.ManagementInterfaces } else { @() }
            targetOwnsIpv4DefaultRoute = if ($target) { $target.OwnsIpv4DefaultRoute } else { $null }
            targetOwnsIpv6DefaultRoute = if ($target) { $target.OwnsIpv6DefaultRoute } else { $null }
            targetIsWifi = if ($target) { $target.IsWifi } else { $null }
            targetIsPhysical = if ($target) { $target.IsPhysical } else { $null }
            baselineMatch = if ($before) { $before.baselineMatch } else { $null }
            diagnosticExitCode = if ($diagnosticResult) { $diagnosticResult.ExitCode } else { $null }
            diagnosticExecutionProven = $diagnosticProven
            beforeAfterEqual = $beforeAfterEqual
            controlledDnsMutationVerified = $null
            controlledDnsRollbackVerified = $null
            controlledIpv4MutationVerified = $null
            controlledIpv4RollbackVerified = $null
            nonTargetMetricDrift = @($mutationTracker.MetricDrift | ForEach-Object { $_ })
            mutationAllowed = $mutationTracker.Allowed
            mutationAttempted = $mutationTracker.Attempted
            mutationPerformed = $mutationTracker.Performed
        }
        $transcript.Add("CompletedAtUtc: $($completed.ToString('o'))")
        $transcript.Add('Success: False')
        $transcript.Add("FailureCode: $failureCode")
        & $Operations.WriteEvidence $evidenceDirectory $summary $before $after $(if ($diagnosticResult) { $diagnosticResult.Output } else { '' }) $transcript
        return [pscustomobject]@{ Success = $false; FailureCode = $failureCode; Target = $target; Summary = $summary; EvidenceDirectory = $evidenceDirectory }
    }
}

function Invoke-IPManHostLab {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [ValidateSet('Preflight', 'DiscoverTarget', 'DnsTruthDiagnostic', 'CollectEvidence', 'ControlledValidation')]
        [string]$Action,
        [string]$ExpectedAdapterGuid,
        [string]$SwitchName = $script:DefaultSwitchName,
        [string]$AliasHint = $script:DefaultAliasHint,
        [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
    )

    $parameters = @{
        Action = $Action
        SwitchName = $SwitchName
        AliasHint = $AliasHint
        RepoRoot = $RepoRoot
        Operations = (New-ProductionHostLabOperations)
    }
    if ($PSBoundParameters.ContainsKey('ExpectedAdapterGuid')) {
        $parameters.ExpectedAdapterGuid = $ExpectedAdapterGuid
    }
    return Invoke-HostLabOrchestration @parameters
}

Export-ModuleMember -Function Invoke-IPManHostLab
