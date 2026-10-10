#Requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-(?:[0-9]*[A-Za-z-][0-9A-Za-z-]*|0|[1-9][0-9]*)(?:\.(?:[0-9]*[A-Za-z-][0-9A-Za-z-]*|0|[1-9][0-9]*))*)?$')]
    [string] $Version,

    [string] $OutputDirectory = (Join-Path $PSScriptRoot '../../artifacts/release'),

    [switch] $SkipInstaller,

    # An empty-source config allows offline validation using cached packages only.
    [string] $NuGetConfigFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$outputRoot = [IO.Path]::GetFullPath($OutputDirectory)
$isccPath = $null

if (-not $SkipInstaller) {
    $iscc = Get-Command ISCC.exe -CommandType Application -ErrorAction SilentlyContinue
    if ($iscc) {
        $isccPath = $iscc.Source
    } else {
        foreach ($programFiles in @(${env:ProgramFiles(x86)}, $env:ProgramFiles)) {
            if ($programFiles) {
                $candidate = Join-Path $programFiles 'Inno Setup 6/ISCC.exe'
                if (Test-Path -LiteralPath $candidate -PathType Leaf) {
                    $isccPath = $candidate
                    break
                }
            }
        }
    }
    if (-not $isccPath) {
        throw 'ISCC.exe bulunamadı. Inno Setup 6.3+ gerekli; yalnızca taşınabilir paket için -SkipInstaller kullanın.'
    }
}

$publishArguments = @(
    'publish', (Join-Path $repoRoot 'src/IPMan.App'),
    '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true',
    "-p:Version=$Version", '-p:DebugSymbols=false', '-p:DebugType=None'
)
if ($NuGetConfigFile) {
    $configPath = (Resolve-Path -LiteralPath $NuGetConfigFile).Path
    $publishArguments += "-p:RestoreConfigFile=$configPath", '-p:NuGetAudit=false'
}

# Never set IPMAN_* variables, invoke tools/host-lab, or launch the application.
New-Item -ItemType Directory -Path $outputRoot -Force | Out-Null
$workRoot = Join-Path $outputRoot ('.work/' + [Guid]::NewGuid().ToString('N'))
$publishDirectory = Join-Path $workRoot 'IPMan'
$portablePath = Join-Path $outputRoot "IPMan-$Version-win-x64-portable.zip"
$installerPath = Join-Path $outputRoot "IPMan-$Version-win-x64-setup.exe"

try {
    New-Item -ItemType Directory -Path $publishDirectory -Force | Out-Null
    & dotnet @publishArguments --output $publishDirectory
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet publish başarısız oldu (çıkış kodu: $LASTEXITCODE)."
    }
    foreach ($requiredFile in @('IPMan.exe', 'LICENSE', 'coreclr.dll', 'hostfxr.dll', 'PresentationFramework.dll')) {
        if (-not (Test-Path -LiteralPath (Join-Path $publishDirectory $requiredFile) -PathType Leaf)) {
            throw "Yayın çıktısı eksik: $requiredFile"
        }
    }
    if (Get-ChildItem -LiteralPath $publishDirectory -Filter '*.pdb' -Recurse) {
        throw 'Yayın çıktısı PDB içeriyor.'
    }

    # The staging root contains exactly one directory: IPMan.
    if (Test-Path -LiteralPath $portablePath) {
        Remove-Item -LiteralPath $portablePath
    }
    [IO.Compression.ZipFile]::CreateFromDirectory($workRoot, $portablePath)
    Write-Host "Taşınabilir paket: $portablePath"

    if (-not $SkipInstaller) {
        if (Test-Path -LiteralPath $installerPath) {
            Remove-Item -LiteralPath $installerPath
        }
        & $isccPath "/DAppVersion=$Version" "/DPublishDirectory=$publishDirectory" "/DPackageDirectory=$outputRoot" (Join-Path $repoRoot 'installer/IPMan.iss')
        if ($LASTEXITCODE -ne 0) {
            throw "ISCC başarısız oldu (çıkış kodu: $LASTEXITCODE)."
        }
        if (-not (Test-Path -LiteralPath $installerPath -PathType Leaf)) {
            throw "Kurulum çıktısı bulunamadı: $installerPath"
        }
        Write-Host "Kurulum paketi: $installerPath"
    }
} finally {
    # Only delete this run's staging directory, after checking the absolute path.
    $resolvedWorkRoot = [IO.Path]::GetFullPath($workRoot)
    $workParent = [IO.Path]::GetFullPath((Join-Path $outputRoot '.work')) + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedWorkRoot.StartsWith($workParent, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Geçici dizin çıktı klasörünün dışında: $resolvedWorkRoot"
    }
    if (Test-Path -LiteralPath $resolvedWorkRoot) {
        Remove-Item -LiteralPath $resolvedWorkRoot -Recurse -Force
    }
}
