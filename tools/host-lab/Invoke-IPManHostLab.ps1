[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Preflight', 'DiscoverTarget', 'DnsTruthDiagnostic', 'CollectEvidence', 'ControlledValidation')]
    [string]$Action,
    [string]$ExpectedAdapterGuid,
    [string]$SwitchName = 'IPMan-Test-Switch',
    [string]$AliasHint = 'vEthernet (IPMan-Test-Switch)'
)

Set-StrictMode -Version 2.0
$modulePath = Join-Path $PSScriptRoot 'IPMan.HostLab.psm1'
Import-Module $modulePath -Force

$parameters = @{
    Action = $Action
    SwitchName = $SwitchName
    AliasHint = $AliasHint
}
if ($PSBoundParameters.ContainsKey('ExpectedAdapterGuid')) {
    $parameters.ExpectedAdapterGuid = $ExpectedAdapterGuid
}

$result = Invoke-IPManHostLab @parameters
$result | ConvertTo-Json -Depth 12
if (-not $result.Success) { exit 1 }
