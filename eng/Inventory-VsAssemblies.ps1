<#
.SYNOPSIS
Inventories managed assemblies shipped with Visual Studio that may constrain Metalama dependency versions.

.DESCRIPTION
Walks key VS payload directories (PrivateAssemblies, PublicAssemblies, Roslyn language service, MSBuild) and
records each managed assembly's identity. Output is a JSON array suitable for diffing across VS patch builds
and for cross-referencing against Directory.Packages.props.

Run on the host (the dev container does not have VS installed). Re-run on a freshly-patched VS install
whenever the floor patch build advances or when the minimum supported VS version changes.

The default version range targets VS 2022 17.14+ Current Channel (the floor for Metalama 2026.1 LTS).
Override with -VsVersionRange or -VsInstallPath when inventorying a different floor.

.PARAMETER VsInstallPath
Override the VS install path. Defaults to the latest install in -VsVersionRange resolved via vswhere.

.PARAMETER VsVersionRange
vswhere version range. Defaults to '[17.14,)' (latest Current Channel patch >= 17.14).

.PARAMETER OutputPath
Where to write the JSON inventory. Defaults to ./vs-assembly-inventory.json in the current directory.

.EXAMPLE
.\eng\Inventory-VsAssemblies.ps1
.\eng\Inventory-VsAssemblies.ps1 -VsVersionRange '[17.14,)' -OutputPath .\eng\vs-17.14-assemblies.json
#>

[CmdletBinding()]
param (
    [string]$VsInstallPath,
    [string]$VsVersionRange = '[17.14,)',
    [string]$OutputPath = (Join-Path (Get-Location) 'vs-assembly-inventory.json')
)

$ErrorActionPreference = 'Stop'

if (-not $VsInstallPath) {
    $vswhere = Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (-not (Test-Path $vswhere)) {
        throw "vswhere.exe not found at $vswhere. Pass -VsInstallPath explicitly."
    }
    $VsInstallPath = & $vswhere -latest -version $VsVersionRange -property installationPath
    if (-not $VsInstallPath) {
        throw "No VS install matching version range $VsVersionRange found. Pass -VsInstallPath explicitly."
    }
}

if (-not (Test-Path $VsInstallPath)) {
    throw "VS install path does not exist: $VsInstallPath"
}

Write-Host "VS install: $VsInstallPath"

# Resolve the full product version of the install we picked, for the manifest header.
$catalogPath = Join-Path $VsInstallPath 'Common7\IDE\Microsoft.VisualStudio.Setup.Configuration.Native.dll'
$productVersion = $null
if (Test-Path $catalogPath) {
    $productVersion = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($catalogPath).ProductVersion
}

# Buckets to scan. Each entry: label + relative path from VS root + whether to recurse.
# Recursion is off by default — PrivateAssemblies subdirectories belong to individual extensions and add noise.
$buckets = @(
    @{ Label = 'PrivateAssemblies'; Path = 'Common7\IDE\PrivateAssemblies'; Recurse = $false },
    @{ Label = 'PublicAssemblies';  Path = 'Common7\IDE\PublicAssemblies';  Recurse = $false },
    @{ Label = 'Roslyn.LanguageService'; Path = 'Common7\IDE\CommonExtensions\Microsoft\VBCSharp\LanguageServices'; Recurse = $false },
    @{ Label = 'MSBuild';           Path = 'MSBuild\Current\Bin';           Recurse = $false },
    @{ Label = 'MSBuild.Roslyn';    Path = 'MSBuild\Current\Bin\Roslyn';    Recurse = $false }
)

$results = New-Object System.Collections.Generic.List[object]

foreach ($bucket in $buckets) {
    $dir = Join-Path $VsInstallPath $bucket.Path
    if (-not (Test-Path $dir)) {
        Write-Warning "Skipping missing directory: $dir"
        continue
    }
    Write-Host "Scanning $($bucket.Label): $dir"

    $files = if ($bucket.Recurse) {
        Get-ChildItem -Path $dir -Filter *.dll -Recurse -ErrorAction SilentlyContinue
    } else {
        Get-ChildItem -Path $dir -Filter *.dll -ErrorAction SilentlyContinue
    }

    foreach ($file in $files) {
        $dll = $file.FullName
        try {
            $asm = [System.Reflection.AssemblyName]::GetAssemblyName($dll)
        }
        catch {
            # Native DLL, mixed-mode, or unreadable — skip.
            continue
        }

        $fvi = [System.Diagnostics.FileVersionInfo]::GetVersionInfo($dll)
        $tokenBytes = $asm.GetPublicKeyToken()
        $token = if ($tokenBytes -and $tokenBytes.Length -gt 0) {
            ($tokenBytes | ForEach-Object { $_.ToString('x2') }) -join ''
        } else { '' }

        $relative = $dll.Substring($VsInstallPath.Length).TrimStart('\')

        $results.Add([pscustomobject]@{
            Category        = $bucket.Label
            AssemblyName    = $asm.Name
            AssemblyVersion = $asm.Version.ToString()
            FileVersion     = $fvi.FileVersion
            ProductVersion  = $fvi.ProductVersion
            PublicKeyToken  = $token
            FileName        = $file.Name
            RelativePath    = $relative
        }) | Out-Null
    }
}

$sorted = $results | Sort-Object Category, AssemblyName, AssemblyVersion, RelativePath

$envelope = [pscustomobject]@{
    GeneratedUtc       = (Get-Date).ToUniversalTime().ToString('o')
    VsInstallPath      = $VsInstallPath
    VsProductVersion   = $productVersion
    VsVersionRange     = $VsVersionRange
    EntryCount         = $sorted.Count
    Assemblies         = $sorted
}

$json = $envelope | ConvertTo-Json -Depth 5
$json | Set-Content -Path $OutputPath -Encoding UTF8

Write-Host ""
Write-Host "Wrote $($sorted.Count) entries to $OutputPath"
Write-Host "VS product version: $productVersion"

$distinct = ($sorted | Group-Object AssemblyName, AssemblyVersion).Count
Write-Host "Distinct (name, version) tuples: $distinct"
