# The original of this file is in the PostSharp.Engineering repo.
# You can generate this file using `./Build.ps1 generate-scripts`.

[CmdletBinding(PositionalBinding = $false)]
param(
    [switch]$Interactive, # Opens an interactive PowerShell session
    [switch]$BuildImage, # Only builds the image, but does not build the product.
    [switch]$NoBuildImage, # Does not build the image.
    [switch]$Clean, # Performs cleanup of bin and obj directories.
    [switch]$NoNuGetCache, # Does not mount the host nuget cache in the container.
    [switch]$KeepEnv, # Does not override the env.g.json file.
    [switch]$Claude, # Run Claude CLI instead of Build.ps1. Use -Claude for interactive, -Claude "prompt" for non-interactive.
    [switch]$NoMcp, # Do not start the MCP approval server (for -Claude mode).
    [switch]$Update, # Update timestamp to invalidate Docker cache and force Claude/plugin updates (Claude mode only).
    [string]$ImageName, # Image name (defaults to a name based on the directory).
    [string]$BuildAgentPath, # Path to build agent directory (defaults based on platform).
    [switch]$LoadEnvFromKeyVault, # Forces loading environment variables form the key vault.
    [switch]$StartVsmon, # Enable the remote debugger.
    [string]$Script = 'Build.ps1', # The build script to be executed inside Docker.
    [string]$Dockerfile, # Path to custom Dockerfile (defaults to Dockerfile or Dockerfile.claude based on -Claude).
    [switch]$NoInit, # Do not generate or call Init.g.ps1 (skips git config, safe.directory, etc).
    [string]$Isolation = 'process', # Docker isolation mode (process or hyperv). Memory/CPU limits only apply to hyperv.
    [string]$Memory, # Docker memory limit (e.g., "8g"). Only used with hyperv isolation.
    [int]$Cpus = [Environment]::ProcessorCount, # Docker CPU limit. Only used with hyperv isolation.
    [string[]]$Mount, # Additional directories to mount from host (readonly by default, append :w for writable). Supports * and ** glob patterns.
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$BuildArgs   # Arguments passed to `Build.ps1` within the container (or Claude prompt if -Claude is specified).
)

####
# These settings are replaced by the generate-scripts command.
$EngPath = 'eng'
$EnvironmentVariables = 'AWS_ACCESS_KEY_ID,AWS_SECRET_ACCESS_KEY,AZ_IDENTITY_USERNAME,AZURE_CLIENT_ID,AZURE_CLIENT_SECRET,AZURE_DEVOPS_TOKEN,AZURE_DEVOPS_USER,AZURE_TENANT_ID,DOC_API_KEY,DOWNLOADS_API_KEY,ENG_USERNAME,GIT_USER_EMAIL,GIT_USER_NAME,GITHUB_AUTHOR_EMAIL,GITHUB_REVIEWER_TOKEN,GITHUB_TOKEN,IS_POSTSHARP_OWNED,IS_TEAMCITY_AGENT,MetalamaLicense,NUGET_ORG_API_KEY,PostSharpLicense,SIGNSERVER_SECRET,TEAMCITY_CLOUD_TOKEN,TYPESENSE_API_KEY,VS_MARKETPLACE_ACCESS_TOKEN,VSS_NUGET_EXTERNAL_FEED_ENDPOINTS'
####

$ErrorActionPreference = "Stop"
$dockerContextDirectory = "$EngPath/docker-context"

# Detect platform (use built-in variables if available, fallback for older PowerShell)
if ($null -eq $IsWindows)
{
    $IsWindows = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT
}
$IsUnix = -not $IsWindows  # Covers both Linux and macOS

# Docker isolation is Windows-only
$isolationArg = if ($IsWindows) { "--isolation=$Isolation" } else { "" }

# Set BuildAgentPath default based on platform
if ([string]::IsNullOrEmpty($BuildAgentPath))
{
    if ($env:TEAMCITY_JRE)
    {
        $BuildAgentPath = Split-Path $env:TEAMCITY_JRE -Parent
    }
    elseif ($IsUnix)
    {
        $BuildAgentPath = '/build-agent'
    }
    else
    {
        $BuildAgentPath = 'C:\BuildAgent'
    }
}

# Capture the calling directory (where the user invoked the script from)
# This will be used as the working directory in the container
$CallingDirectory = (Get-Location).Path

# Resolve Dockerfile path relative to original current directory (before changing location)
# This must be done before Set-Location to preserve the user's intended relative path
if ($Dockerfile -and -not [System.IO.Path]::IsPathRooted($Dockerfile))
{
    $Dockerfile = Join-Path $CallingDirectory $Dockerfile
}

# Save current location and restore on exit
Push-Location
try
{
    Set-Location $PSScriptRoot

    if ($env:IS_TEAMCITY_AGENT)
{
    Write-Host "Running on TeamCity agent at '$BuildAgentPath'" -ForegroundColor Cyan
}

# Function to create secrets JSON file
function New-EnvJson
{
    param(
        [string]$EnvironmentVariableList
    )

    # Parse comma-separated environment variable names
    $envVarNames = $EnvironmentVariableList -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }

    # Build hashtable with environment variable values
    $envVariables = @{ }
    foreach ($envVarName in $envVarNames)
    {
        $value = [Environment]::GetEnvironmentVariable($envVarName)
        if (-not [string]::IsNullOrEmpty($value))
        {
            $envVariables[$envVarName] = $value
        }
    }

    # Add NUGET_PACKAGES with default if not set
    if (-not $envVariables.ContainsKey("NUGET_PACKAGES"))
    {
        $nugetPackages = $env:NUGET_PACKAGES
        if ( [string]::IsNullOrEmpty($nugetPackages))
        {
            if ($IsUnix)
            {
                $nugetPackages = Join-Path $env:HOME ".nuget/packages"
            }
            else
            {
                $nugetPackages = Join-Path $env:USERPROFILE ".nuget\packages"
            }
        }
        $envVariables["NUGET_PACKAGES"] = $nugetPackages
    }

    # Add secrets from the PostSharpBuildEnv key vault, on our development machines.
    # On CI agents, these environment variables are supposed to be set by the host.
    if ($LoadEnvFromKeyVault -or ($env:IS_POSTSHARP_OWNED -and -not $env:IS_TEAMCITY_AGENT))
    {
        $moduleName = "Az.KeyVault"

        if (-not (Get-Module -ListAvailable -Name $moduleName))
        {
            Write-Error "The required module '$moduleName' is not installed. Please install it with: Install-Module -Name $moduleName"
            exit 1
        }

        Import-Module $moduleName
        foreach ($secret in Get-AzKeyVaultSecret -VaultName "PostSharpBuildEnv")
        {
            $secretWithValue = Get-AzKeyVaultSecret -VaultName "PostSharpBuildEnv" -Name $secret.Name
            $envName = $secretWithValue.Name -Replace "-", "_"
            $envValue = (ConvertFrom-SecureString $secretWithValue.SecretValue -AsPlainText)
            $envVariables[$envName] = $envValue
        }
    }

    # Convert to JSON and save
    $gDirectory = Join-Path $dockerContextDirectory ".g"
    $jsonPath = Join-Path $gDirectory "env.g.json"

    # Ensure the directory exists
    if (-not (Test-Path $gDirectory))
    {
        New-Item -ItemType Directory -Path $gDirectory -Force | Out-Null
    }

    # Write a test JSON file with GUID first
    @{ guid = [System.Guid]::NewGuid().ToString() } | ConvertTo-Json | Set-Content -Path $jsonPath -Encoding UTF8

    # Check if secrets file is tracked by git
    $gitStatus = git status --porcelain $jsonPath 2> $null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($gitStatus))
    {
        Write-Error "Secrets file '$jsonPath' is tracked by git. Please add it to .gitignore first."
        exit 1
    }

    $envVariables | ConvertTo-Json -Depth 10 | Set-Content -Path $jsonPath -Encoding UTF8
    Write-Host "Created secrets file: $jsonPath" -ForegroundColor Cyan


    return $jsonPath
}

# Function to create Claude-specific env.g.json with filtered/renamed variables
function New-ClaudeEnvJson
{
    $claudeEnv = @{ }

    # CLAUDE_GITHUB_TOKEN -> GITHUB_TOKEN (renamed)
    if ($env:CLAUDE_GITHUB_TOKEN)
    {
        $claudeEnv["GITHUB_TOKEN"] = $env:CLAUDE_GITHUB_TOKEN
    }

    # Preserved variables
    if ($env:ANTHROPIC_API_KEY)
    {
        $claudeEnv["ANTHROPIC_API_KEY"] = $env:ANTHROPIC_API_KEY
    }
    if ($env:CLAUDE_CODE_OAUTH_TOKEN)
    {
        $claudeEnv["CLAUDE_CODE_OAUTH_TOKEN"] = $env:CLAUDE_CODE_OAUTH_TOKEN
    }
    if ($env:IS_POSTSHARP_OWNED)
    {
        $claudeEnv["IS_POSTSHARP_OWNED"] = $env:IS_POSTSHARP_OWNED
    }
    if ($env:IS_TEAMCITY_AGENT)
    {
        $claudeEnv["IS_TEAMCITY_AGENT"] = $env:IS_TEAMCITY_AGENT
    }

    # Git identity - read from host git config if not set in environment
    $gitUserName = $env:GIT_USER_NAME
    $gitUserEmail = $env:GIT_USER_EMAIL
    if (-not $gitUserName)
    {
        $gitUserName = git config --global user.name
    }
    if (-not $gitUserEmail)
    {
        $gitUserEmail = git config --global user.email
    }
    if ($gitUserName)
    {
        $claudeEnv["GIT_USER_NAME"] = $gitUserName
    }
    if ($gitUserEmail)
    {
        $claudeEnv["GIT_USER_EMAIL"] = $gitUserEmail
    }

    # Add NUGET_PACKAGES with default if not set
    $nugetPackages = $env:NUGET_PACKAGES
    if ( [string]::IsNullOrEmpty($nugetPackages))
    {
        if ($IsUnix)
        {
            $nugetPackages = Join-Path $env:HOME ".nuget/packages"
        }
        else
        {
            $nugetPackages = Join-Path $env:USERPROFILE ".nuget\packages"
        }
    }
    $claudeEnv["NUGET_PACKAGES"] = $nugetPackages

    # Convert to JSON and save
    $gDirectory = Join-Path $dockerContextDirectory ".g"
    $jsonPath = Join-Path $gDirectory "env.g.json"

    # Ensure the directory exists
    if (-not (Test-Path $gDirectory))
    {
        New-Item -ItemType Directory -Path $gDirectory -Force | Out-Null
    }

    # Write a test JSON file with GUID first
    @{ guid = [System.Guid]::NewGuid().ToString() } | ConvertTo-Json | Set-Content -Path $jsonPath -Encoding UTF8

    # Check if secrets file is tracked by git
    $gitStatus = git status --porcelain $jsonPath 2> $null
    if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($gitStatus))
    {
        Write-Error "Secrets file '$jsonPath' is tracked by git. Please add it to .gitignore first."
        exit 1
    }

    $claudeEnv | ConvertTo-Json -Depth 10 | Set-Content -Path $jsonPath -Encoding UTF8
    Write-Host "Created Claude secrets file: $jsonPath" -ForegroundColor Cyan

    return $jsonPath
}

# Fixed port for MCP approval server (must match McpHttpServer.FixedPort)
$mcpFixedPort = 9847

# Function to check if the MCP approval server is running
function Test-McpServerRunning
{
    param(
        [int]$Port = $mcpFixedPort
    )

    try
    {
        $response = Invoke-WebRequest -Uri "http://localhost:$Port/health" -TimeoutSec 10 -ErrorAction Stop
        return $response.StatusCode -eq 200
    }
    catch
    {
        return $false
    }
}

function Get-TimestampFile
{
    param(
        [switch]$Update
    )

    $timestampDir = if ($IsUnix)
    {
        Join-Path $env:HOME ".local/share/PostSharp.Engineering"
    }
    else
    {
        Join-Path $env:LOCALAPPDATA "PostSharp.Engineering"
    }
    $timestampFile = Join-Path $timestampDir "update.timestamp"

    # Ensure directory exists
    if (-not (Test-Path $timestampDir))
    {
        New-Item -ItemType Directory -Path $timestampDir -Force | Out-Null
    }

    # Create file if it doesn't exist OR update if -Update specified
    if (-not (Test-Path $timestampFile) -or $Update)
    {
        $timestamp = [DateTime]::UtcNow.ToString("o")  # ISO 8601 format
        Set-Content -Path $timestampFile -Value $timestamp -NoNewline -Force
        Write-Host "Timestamp file updated: $timestamp" -ForegroundColor Cyan
    }

    return $timestampFile
}

# Dictionary to track volume mounts with "writable wins" logic
$script:VolumeMountDict = @{}

function Add-VolumeMount {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Path,
        [switch]$Writable
    )

    $normalizedPath = $Path.TrimEnd('\', '/')
    $normalizedKey = $normalizedPath.ToLower()
    $isGitDirectory = Test-Path (Join-Path $normalizedPath ".git")

    if ($script:VolumeMountDict.ContainsKey($normalizedKey)) {
        if ($Writable) {
            $script:VolumeMountDict[$normalizedKey].Writable = $true
        }
    } else {
        $script:VolumeMountDict[$normalizedKey] = @{
            HostPath = $normalizedPath
            Writable = [bool]$Writable
            IsGitDirectory = $isGitDirectory
        }
    }
}

if ($env:RUNNING_IN_DOCKER)
{
    Write-Error "Already running in Docker."
    exit 1
}

# Determine which Dockerfile will be used (needed for ImageName generation)
if (-not $Dockerfile)
{
    if ($Claude)
    {
        $Dockerfile = "Dockerfile.claude"
    }
    else
    {
        $Dockerfile = "Dockerfile"
    }
}

# Get the full path of the Dockerfile
if ([System.IO.Path]::IsPathRooted($Dockerfile))
{
    $dockerfileFullPath = $Dockerfile
}
else
{
    $dockerfileFullPath = Join-Path $PSScriptRoot $Dockerfile
}

# Generate a hash of the Dockerfile full path (4 bytes, 8 hex chars)
$hashBytes = (New-Object -TypeName System.Security.Cryptography.SHA256Managed).ComputeHash([System.Text.Encoding]::UTF8.GetBytes($dockerfileFullPath))
$dockerfileHash = [System.BitConverter]::ToString($hashBytes, 0, 4).Replace("-", "").ToLower()

# Generate ImageTag using the hash
if ( [string]::IsNullOrEmpty($ImageName))
{
    $ImageTag = "dockerfile-$dockerfileHash"
    Write-Host "Generated image tag from Dockerfile path hash: $ImageTag" -ForegroundColor Cyan
}
else
{
    $ImageTag = "$ImageName`:$dockerfileHash"
    Write-Host "Image will be tagged as: $ImageTag" -ForegroundColor Cyan
}

# Check MCP server availability for -Claude mode
# The MCP approval server is now a standalone GUI app that must be started separately
$mcpServerAvailable = $false
if ($Claude -and -not $NoMcp)
{
    if (Test-McpServerRunning)
    {
        Write-Host "MCP approval server detected on port $mcpFixedPort" -ForegroundColor Cyan
        $mcpServerAvailable = $true
    }
    else
    {
        Write-Warning "MCP approval server not running on port $mcpFixedPort."
        Write-Warning "Start PostSharp.Engineering.McpApprovalServer.exe before using -Claude mode for host operations."
        Write-Warning "Continuing without MCP server support."
    }
}

# When building locally (as opposed as on the build agent), we can optionally do a complete cleanup because
# obj files may point to the host filesystem.
if ($Clean)
{
    Write-Host "Cleaning up." -ForegroundColor Green
    Get-ChildItem "bin" -Recurse | Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
    Get-ChildItem "obj" -Recurse | Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
}

Write-Host "Preparing context and mounts." -ForegroundColor Green
# Create secrets JSON file.
if (-not $KeepEnv)
{
    if ($Claude)
    {
        # Use Claude-specific environment variables (filtered and renamed)
        New-ClaudeEnvJson

        # Get/update timestamp file for cache invalidation (only if building image)
        if (-not $NoBuildImage)
        {
            $timestampFile = Get-TimestampFile -Update:$Update
        }
    }
    else
    {
        # Use standard build environment variables
        if (-not $env:ENG_USERNAME)
        {
            $env:ENG_USERNAME = $env:USERNAME
        }

        # Add git identity to environment
        if ($env:IS_TEAMCITY_AGENT)
        {
            # On TeamCity agents, check if the environment variables are set.
            if (-not $env:GIT_USER_EMAIL -or -not $env:GIT_USER_NAME)
            {
                Write-Error "On TeamCity agents, the GIT_USER_EMAIL and GIT_USER_NAME environment variables must be set."
                exit 1
            }
        }
        else
        {
            # On developer machines, use the current git user.
            $env:GIT_USER_EMAIL = git config --global user.email
            $env:GIT_USER_NAME = git config --global user.name
        }

        New-EnvJson -EnvironmentVariableList $EnvironmentVariables
    }
}

# Get the source directory name from $PSScriptRoot (script location)
$SourceDirName = $PSScriptRoot

# Start timing the entire process except cleaning
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# Ensure docker context directory exists and contains at least one file
if (-not (Test-Path $dockerContextDirectory))
{
    Write-Error "Docker context directory '$dockerContextDirectory' does not exist."
    exit 1
}


# Container user profile (matches actual user in container)
$containerUserProfile = if ($IsUnix) { "/root" } else { "C:\Users\ContainerAdministrator" }

# Initialize arrays for special mounts (those with different host/container paths)
$VolumeMappings = @()
$MountPoints = @()
$GitDirectories = @()

# Prepare volume mappings using the dictionary
Add-VolumeMount -Path $SourceDirName -Writable

# Define static Git system directory for mapping. This used by Teamcity as an LFS parent repo.
$gitSystemDir = "$BuildAgentPath\system\git"

if (Test-Path $gitSystemDir)
{
    Add-VolumeMount -Path $gitSystemDir
}

# Mount the host NuGet cache in the container.
if (-not $NoNuGetCache)
{
    # Use NUGET_PACKAGES from environment or default to user profile
    $nugetCacheDir = $env:NUGET_PACKAGES
    if ( [string]::IsNullOrEmpty($nugetCacheDir))
    {
        if ($IsUnix)
        {
            $nugetCacheDir = Join-Path $env:HOME ".nuget/packages"
        }
        else
        {
            $nugetCacheDir = Join-Path $env:USERPROFILE ".nuget\packages"
        }
    }

    Write-Host "NuGet cache directory: $nugetCacheDir" -ForegroundColor Cyan
    if (-not (Test-Path $nugetCacheDir))
    {
        Write-Host "Creating NuGet cache directory on host: $nugetCacheDir"
        New-Item -ItemType Directory -Force -Path $nugetCacheDir | Out-Null
    }

    # Mount to the same path in the container (will be transformed by Get-ContainerPath later)
    Add-VolumeMount -Path $nugetCacheDir -Writable
}

# Mount PostSharp.Engineering data directory (for version counters)
$hostEngineeringDataDir = if ($IsUnix)
{
    Join-Path $env:HOME ".local/share/PostSharp.Engineering"
}
else
{
    Join-Path $env:LOCALAPPDATA "PostSharp.Engineering"
}

if (-not (Test-Path $hostEngineeringDataDir))
{
    New-Item -ItemType Directory -Force -Path $hostEngineeringDataDir | Out-Null
}

$containerEngineeringDataDir = if ($IsUnix)
{
    Join-Path $containerUserProfile ".local/share/PostSharp.Engineering"
}
else
{
    Join-Path $containerUserProfile "AppData\Local\PostSharp.Engineering"
}
$VolumeMappings += "${hostEngineeringDataDir}:${containerEngineeringDataDir}"
$MountPoints += $containerEngineeringDataDir

# Mount VS Remote Debugger
if ($StartVsmon)
{
    if (-not $env:DevEnvDir)
    {
        Write-Host "Environment variable 'DevEnvDir' is not defined." -ForegroundColor Red
        exit 1
    }

    $remoteDebuggerHostDir = "$( $env:DevEnvDir )Remote Debugger\x64"
    if (-not (Test-Path $remoteDebuggerHostDir))
    {
        Write-Host "Directory '$remoteDebuggerHostDir' does not exist." -ForegroundColor Red
        exit 1
    }

    $remoteDebuggerContainerDir = "C:\msvsmon"
    $VolumeMappings += "${remoteDebuggerHostDir}:${remoteDebuggerContainerDir}:ro"
    $MountPoints += $remoteDebuggerContainerDir

}

# Discover symbolic links in source-dependencies and add their targets to mount points
$sourceDependenciesDir = Join-Path $SourceDirName "source-dependencies"
if (Test-Path $sourceDependenciesDir)
{
    $symbolicLinks = Get-ChildItem -Path $sourceDependenciesDir -Force | Where-Object { $_.LinkType -eq 'SymbolicLink' }

    foreach ($link in $symbolicLinks)
    {
        $targetPath = $link.Target
        if (-not [string]::IsNullOrEmpty($targetPath) -and (Test-Path $targetPath))
        {
            Write-Host "Found symbolic link '$( $link.Name )' -> '$targetPath'" -ForegroundColor Cyan
            Add-VolumeMount -Path $targetPath
        }
        else
        {
            Write-Host "Warning: Symbolic link '$( $link.Name )' target '$targetPath' does not exist or is invalid" -ForegroundColor Yellow
        }
    }

    $sourceDirectories = Get-ChildItem -Path $sourceDependenciesDir -Force | Where-Object { $_.LinkType -eq $null }
    foreach ($sourceDirectory in $sourceDirectories)
    {
        Write-Host "Mounting source-dependencies directory: $($sourceDirectory.FullName)" -ForegroundColor Cyan
        $GitDirectories += $sourceDirectory.FullName    
    }
}

# Mount sibling directories from the product family (parent directory)
# Only if parent is a recognized product family (PostSharp* or Metalama*)
$parentDir = Split-Path $SourceDirName -Parent
$parentDirName = Split-Path $parentDir -Leaf
if ($parentDir -and (Test-Path $parentDir) -and ($parentDirName -like "PostSharp*" -or $parentDirName -like "Metalama*"))
{
    Write-Host "Detected product family directory: $parentDirName" -ForegroundColor Cyan
    $siblingDirs = Get-ChildItem -Path $parentDir -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -ne $SourceDirName }

    foreach ($sibling in $siblingDirs)
    {
        $siblingPath = $sibling.FullName
        Write-Host "Mounting product family sibling: $siblingPath" -ForegroundColor Cyan
        Add-VolumeMount -Path $siblingPath
    }
}

# Mount PostSharp.Engineering.* directories from grandparent
# This provides access to engineering tools and related repos
$grandparentDir = Split-Path $parentDir -Parent
if ($grandparentDir -and (Test-Path $grandparentDir))
{
    $engineeringDirs = Get-ChildItem -Path $grandparentDir -Directory -Filter "PostSharp.Engineering*" -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -ne $SourceDirName }

    foreach ($engDir in $engineeringDirs)
    {
        $engDirPath = $engDir.FullName
        Write-Host "Mounting engineering repo: $engDirPath" -ForegroundColor Cyan
        Add-VolumeMount -Path $engDirPath
    }
}

# Process -Mount parameter for additional directory mounts
if ($Mount -and $Mount.Count -gt 0)
{
    foreach ($mountSpec in $Mount)
    {
        # Check if writable (ends with :w)
        $isWritable = $false
        $pattern = $mountSpec
        if ($mountSpec -match ':w$')
        {
            $isWritable = $true
            $pattern = $mountSpec -replace ':w$', ''
        }

        # Trim trailing slashes
        $pattern = $pattern.TrimEnd('\', '/')

        # Check if pattern contains glob characters
        if ($pattern -match '\*')
        {
            # Expand glob pattern to match directories only
            # Get the base directory (everything before the first glob)
            $patternParts = $pattern -split '[\\/]'
            $basePathParts = @()
            $globStartIndex = -1

            for ($i = 0; $i -lt $patternParts.Count; $i++)
            {
                if ($patternParts[$i] -match '\*')
                {
                    $globStartIndex = $i
                    break
                }
                $basePathParts += $patternParts[$i]
            }

            if ($basePathParts.Count -gt 0)
            {
                $basePath = $basePathParts -join [System.IO.Path]::DirectorySeparatorChar
            }
            else
            {
                $basePath = "."
            }

            if (Test-Path $basePath)
            {
                # Determine if recursive search is needed (pattern contains **)
                $isRecursive = $pattern -match '\*\*'

                # Build the glob pattern for the part after the base path
                $globPart = ($patternParts[$globStartIndex..($patternParts.Count - 1)]) -join [System.IO.Path]::DirectorySeparatorChar

                # Get matching directories
                $matchingDirs = @()
                if ($isRecursive)
                {
                    # For ** patterns, recurse and convert ** to * for -like matching
                    # Replace ** with a regex-friendly pattern for matching
                    $likePattern = $pattern -replace '\*\*', '*'
                    $matchingDirs = Get-ChildItem -Path $basePath -Directory -Recurse -ErrorAction SilentlyContinue |
                        Where-Object { $_.FullName -like $likePattern }
                }
                else
                {
                    # For single * patterns, use direct matching without recursion
                    $matchingDirs = Get-ChildItem -Path $basePath -Directory -ErrorAction SilentlyContinue |
                        Where-Object { $_.FullName -like $pattern }
                }

                if ($matchingDirs.Count -eq 0)
                {
                    Write-Host "Warning: No directories matched pattern '$pattern'" -ForegroundColor Yellow
                }
                else
                {
                    foreach ($dir in $matchingDirs)
                    {
                        $dirPath = $dir.FullName
                        $rwStatus = if ($isWritable) { "writable" } else { "readonly" }
                        Write-Host "Mounting from -Mount pattern '$pattern': $dirPath ($rwStatus)" -ForegroundColor Cyan
                        Add-VolumeMount -Path $dirPath -Writable:$isWritable
                    }
                }
            }
            else
            {
                Write-Host "Warning: Base path '$basePath' for pattern '$pattern' does not exist" -ForegroundColor Yellow
            }
        }
        else
        {
            # No glob - mount directly if it's a directory
            if (Test-Path $pattern -PathType Container)
            {
                $rwStatus = if ($isWritable) { "writable" } else { "readonly" }
                Write-Host "Mounting from -Mount: $pattern ($rwStatus)" -ForegroundColor Cyan
                Add-VolumeMount -Path $pattern -Writable:$isWritable
            }
            else
            {
                Write-Host "Warning: Mount path '$pattern' does not exist or is not a directory" -ForegroundColor Yellow
            }
        }
    }
}

# Convert dictionary entries to arrays (with "writable wins" deduplication already applied)
# Sort by key for deterministic ordering to optimize Docker image layer reuse
foreach ($key in $script:VolumeMountDict.Keys | Sort-Object) {
    $entry = $script:VolumeMountDict[$key]
    $mountOption = if ($entry.Writable) { "" } else { ":ro" }
    $VolumeMappings += "$($entry.HostPath):$($entry.HostPath)$mountOption"
    $MountPoints += $entry.HostPath
    if ($entry.IsGitDirectory) {
        $GitDirectories += $entry.HostPath
    }
}

# Execute auto-generated DockerMounts.g.ps1 script to add more directory mounts.
$dockerMountsScript = Join-Path $EngPath 'DockerMounts.g.ps1'
if (Test-Path $dockerMountsScript)
{
    Write-Host "Importing Docker mount points from $dockerMountsScript" -ForegroundColor Cyan
    . $dockerMountsScript

    # Check if we need to convert Windows paths to WSL paths
    # This happens when DockerMounts.g.ps1 was generated on Windows but we're running on WSL
    if ($IsUnix)
    {
        # Check if any volume mapping contains Windows-style paths (e.g., C:\)
        $hasWindowsPaths = $VolumeMappings | Where-Object { $_ -match '^[A-Za-z]:\\' }

        if ($hasWindowsPaths)
        {
            Write-Host "Detected Windows paths in DockerMounts.g.ps1 while running on Unix. Converting paths to WSL format." -ForegroundColor Yellow

            # Function to convert Windows path to WSL path
            function ConvertTo-WslPath {
                param([string]$WindowsPath)

                if ($WindowsPath -match '^([A-Za-z]):\\(.*)$')
                {
                    $drive = $Matches[1].ToLower()
                    $path = $Matches[2] -replace '\\', '/'
                    return "/mnt/$drive/$path"
                }
                return $WindowsPath
            }

            # Convert VolumeMappings
            # Note: When running Docker Desktop for Windows from WSL, BOTH host and container paths
            # need to be in WSL format (/mnt/c/...) because Docker is invoked from WSL context.
            $convertedVolumeMappings = @()
            foreach ($mapping in $VolumeMappings)
            {
                # Parse mapping: hostPath:containerPath[:options]
                # Challenge: colons appear in Windows paths (C:\) and as delimiters
                # Strategy: Split on : and reconstruct Windows paths (single letter followed by \ path)
                $parts = $mapping -split ':'

                $i = 0

                # Extract host path
                if ($parts[$i].Length -eq 1 -and $i+1 -lt $parts.Length -and $parts[$i+1] -match '^[\\/]')
                {
                    # Windows path: C:\path - convert to WSL format
                    $hostPath = "$($parts[$i]):$($parts[$i+1])"
                    $hostPath = ConvertTo-WslPath $hostPath
                    $i += 2
                }
                else
                {
                    # Unix path: /path - keep as-is
                    $hostPath = $parts[$i]
                    $i += 1
                }

                # Extract container path
                if ($i -lt $parts.Length)
                {
                    if ($parts[$i].Length -eq 1 -and $i+1 -lt $parts.Length -and $parts[$i+1] -match '^[\\/]')
                    {
                        # Windows path - convert to WSL format
                        $containerPath = "$($parts[$i]):$($parts[$i+1])"
                        $containerPath = ConvertTo-WslPath $containerPath
                        $i += 2
                    }
                    else
                    {
                        # Unix path - keep as-is
                        $containerPath = $parts[$i]
                        $i += 1
                    }
                }
                else
                {
                    $containerPath = $hostPath  # Fallback
                }

                # Rest is options (:ro or :rw)
                if ($i -lt $parts.Length)
                {
                    $options = ':' + ($parts[$i..($parts.Length-1)] -join ':')
                }
                else
                {
                    $options = ''
                }

                $convertedVolumeMappings += "${hostPath}:${containerPath}${options}"
            }
            $VolumeMappings = $convertedVolumeMappings

            # Convert MountPoints
            $MountPoints = $MountPoints | ForEach-Object { ConvertTo-WslPath $_ }

            # Convert GitDirectories
            $GitDirectories = $GitDirectories | ForEach-Object { ConvertTo-WslPath $_ }
        }
    }
}
elseif (-not $env:IS_TEAMCITY_AGENT)
{
    Write-Error "DockerMounts.g.ps1 not found at '$dockerMountsScript'. Run './Build.ps1 prepare' or './Build.ps1 dependencies update' to generate it."
    exit 1
}

# Handle path transformations (platform-specific)
$substCommandsInline = ""

if ($IsWindows)
{
    # Handle non-C: drive letters for Docker (Windows containers only have C: by default)
    # We mount X:\foo to C:\X\foo in the container, then use subst to create the X: drive
    $driveLetters = @{ }

    function Get-ContainerPath($hostPath)
    {
        if ($hostPath -match '^([A-Za-z]):(.*)$')
        {
            $driveLetter = $Matches[1].ToUpper()
            $pathWithoutDrive = $Matches[2]
            if ($driveLetter -ne 'C')
            {
                $driveLetters[$driveLetter] = $true
                return "C:\$driveLetter$pathWithoutDrive"
            }
        }
        return $hostPath
    }

    # Transform all volume mappings to use container paths
    $transformedVolumeMappings = @()
    foreach ($mapping in $VolumeMappings)
    {
        # Parse volume mapping: hostPath:containerPath[:options]
        if ($mapping -match '^([A-Za-z]:\\[^:]*):([A-Za-z]:\\[^:]*)(:.+)?$')
        {
            $hostPath = $Matches[1]
            $containerPath = $Matches[2]
            $options = $Matches[3]
            $newContainerPath = Get-ContainerPath $containerPath
            $transformedVolumeMappings += "${hostPath}:${newContainerPath}${options}"
        }
        else
        {
            $transformedVolumeMappings += $mapping
        }
    }
    $VolumeMappings = $transformedVolumeMappings

    # Transform MountPoints, GitDirectories, SourceDirName, and CallingDirectory for the container
    $MountPoints = $MountPoints | ForEach-Object { Get-ContainerPath $_ }
    $GitDirectories = $GitDirectories | ForEach-Object { Get-ContainerPath $_ }
    $ContainerSourceDir = Get-ContainerPath $SourceDirName
    $ContainerCallingDir = Get-ContainerPath $CallingDirectory

    # Add both the unmapped (C:\X\...) and mapped (X:\...) paths to GitDirectories for safe.directory
    # Git may resolve paths differently depending on how it's invoked
    $expandedGitDirectories = @()
    foreach ($dir in $GitDirectories)
    {
        $expandedGitDirectories += $dir
        # If path is C:\<letter>\... (unmapped subst path), also add <letter>:\... (mapped path)
        if ($dir -match '^C:\\([A-Za-z])\\(.*)$')
        {
            $letter = $Matches[1].ToUpper()
            $rest = $Matches[2]
            $expandedGitDirectories += "${letter}:\$rest"
        }
    }
    $GitDirectories = $expandedGitDirectories

    # Deduplicate again after transformations and expansions (case-insensitive for Windows paths)
    $VolumeMappings = $VolumeMappings | Group-Object { $_.ToLower() } | ForEach-Object { $_.Group[0] }
    $MountPoints = $MountPoints | Group-Object { $_.ToLower() } | ForEach-Object { $_.Group[0] }
    $GitDirectories = $GitDirectories | Group-Object { "$_".ToLower() } | ForEach-Object { $_.Group[0] }

    # Build subst commands string for inline execution in docker run
    foreach ($letter in $driveLetters.Keys | Sort-Object)
    {
        $substCommandsInline += "C:\Windows\System32\subst.exe ${letter}: C:\$letter; "
    }
    if ($driveLetters.Keys.Count -gt 0)
    {
        Write-Host "Drive letter mappings for container: $( $driveLetters.Keys -join ', ' )" -ForegroundColor Cyan
    }
}
else
{
    # Unix (Linux/macOS): No drive letter mapping needed, paths remain as-is
    $ContainerSourceDir = $SourceDirName
    $ContainerCallingDir = $CallingDirectory

    # Deduplicate (case-sensitive for Unix paths)
    $VolumeMappings = $VolumeMappings | Sort-Object -Unique
    $MountPoints = $MountPoints | Sort-Object -Unique
    $GitDirectories = $GitDirectories | Sort-Object -Unique
}

# Create Init.g.ps1 with git configuration (safe.directory and user identity)
if (-not $NoInit)
{
    $gDirectory = Join-Path $dockerContextDirectory ".g"
    if (-not (Test-Path $gDirectory))
    {
        New-Item -ItemType Directory -Path $gDirectory -Force | Out-Null
    }
    $initScript = Join-Path $gDirectory "Init.g.ps1"
    $initScriptContent = @"
# Auto-generated initialization script for container startup

# Configure git user identity from Machine environment variables
`$gitUserName = [Environment]::GetEnvironmentVariable('GIT_USER_NAME', 'Machine')
`$gitUserEmail = [Environment]::GetEnvironmentVariable('GIT_USER_EMAIL', 'Machine')
if (`$gitUserName) {
    git config --global user.name `$gitUserName
}
if (`$gitUserEmail) {
    git config --global user.email `$gitUserEmail
}

# Disable autocrlf to prevent EOL conversion issues with Claude Code's Edit tool
git config --global core.autocrlf false

# Configure git safe.directory for all mounted directories
`$gitDirectories = @(
$( ($GitDirectories | ForEach-Object { "    '$_'" }) -join ",`n" )
)

foreach (`$dir in `$gitDirectories) {
    if (`$dir) {
        # Normalize path: convert backslashes to forward slashes, add trailing slash
        `$normalizedDir = (`$dir -replace '\\\\', '/').TrimEnd('/') + '/'
        git config --global --add safe.directory `$normalizedDir
    }
}

"@
    $initScriptContent | Set-Content -Path $initScript -Encoding UTF8
}

# Copy timestamp file to docker context (for Claude mode cache invalidation)
if ($Claude -and $timestampFile)
{
    $gDirectory = Join-Path $dockerContextDirectory ".g"
    if (-not (Test-Path $gDirectory))
    {
        New-Item -ItemType Directory -Path $gDirectory -Force | Out-Null
    }
    $timestampDestination = Join-Path $gDirectory "update.timestamp"
    Copy-Item -Path $timestampFile -Destination $timestampDestination -Force
    Write-Host "Copied timestamp file to docker context" -ForegroundColor Cyan
}

# Path separator depends on platform (and container OS)
$pathSeparator = if ($IsUnix) { ":" } else { ";" }
$mountPointsAsString = $MountPoints -Join $pathSeparator
$gitDirectoriesAsString = $GitDirectories -Join $pathSeparator

Write-Host "Volume mappings: " @VolumeMappings -ForegroundColor Gray
Write-Host "Mount points: " $mountPointsAsString -ForegroundColor Gray
Write-Host "Git directories: " $gitDirectoriesAsString -ForegroundColor Gray

# Check if a container is already running with this image (only for interactive scenarios)
$existingContainerId = $null

if ($Interactive)
{
    # Check for existing container
    $existingContainerId = docker ps -q --filter "ancestor=$ImageTag" | Select-Object -First 1
    if ($existingContainerId)
    {
        Write-Host "Found existing container $existingContainerId running with image $ImageTag" -ForegroundColor Cyan
        Write-Host "Will reuse existing container instead of starting a new one." -ForegroundColor Cyan
        $ImageTag = $searchImageTag
    }
    else
    {
        Write-Host "No existing container for $ImageTag."
    }
}

# If no existing container, kill any stopped containers with same image to avoid conflicts
if (-not $existingContainerId)
{
    docker ps -q --filter "ancestor=$ImageTag" | ForEach-Object {
        Write-Host "Killing container $_"
        docker kill $_
    }
}

# Building the image.
if (-not $NoBuildImage -and -not $existingContainerId)
{

    Write-Host "Using Dockerfile: $Dockerfile" -ForegroundColor Cyan

    # Read the dockerfile content
    $dockerfileContent = Get-Content -Raw $Dockerfile

    # Check if the dockerfile has mountpoints creation code
    if ($dockerfileContent -notmatch 'ARG MOUNTPOINTS')
    {
        Write-Host "Dockerfile does not have mountpoints creation code. Appending mountpoints setup." -ForegroundColor Yellow

        # Append hardcoded mountpoints creation code (platform-specific)
        if ($IsWindows)
        {
            # Windows container (PowerShell)
            $mountpointsCode = @"

# Create directories for mountpoints
ARG MOUNTPOINTS
RUN if (`$env:MOUNTPOINTS) { ``
        `$mounts = `$env:MOUNTPOINTS -split ';'; ``
        foreach (`$dir in `$mounts) { ``
            if (`$dir) { ``
                Write-Host "Creating directory `$dir``."; ``
                New-Item -ItemType Directory -Path `$dir -Force | Out-Null; ``
            } ``
        } ``
    }
"@
        }
        else
        {
            # Unix container (POSIX sh-compatible)
            $mountpointsCode = @"

# Create directories for mountpoints
ARG MOUNTPOINTS
RUN if [ -n "`$MOUNTPOINTS" ]; then \
        OLD_IFS="`$IFS"; \
        IFS=':'; \
        set -- `$MOUNTPOINTS; \
        IFS="`$OLD_IFS"; \
        for dir in "`$@"; do \
            if [ -n "`$dir" ]; then \
                echo "Creating directory `$dir."; \
                mkdir -p "`$dir"; \
            fi; \
        done; \
    fi
"@
        }
        $dockerfileContent += $mountpointsCode
        Write-Host "Appended mountpoints creation code" -ForegroundColor Cyan
    }

    Write-Host "Building the image with tag: $ImageTag" -ForegroundColor Green

    # Build docker build command with optional --memory (not supported in process isolation)
    $dockerBuildCmd = @('build', '-t', $ImageTag)
    if ($Memory -and $Isolation -ne 'process') {
        $dockerBuildCmd += "--memory=$Memory"
    }
    $dockerBuildCmd += @('--build-arg', "MOUNTPOINTS=$mountPointsAsString", '-f', '-', $dockerContextDirectory)

    $dockerfileContent | & docker @dockerBuildCmd
    if ($LASTEXITCODE -ne 0)
    {
        Write-Host "Docker build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit $LASTEXITCODE
    }
}
else
{
    if ($existingContainerId)
    {
        Write-Host "Skipping image build (reusing existing container $existingContainerId)." -ForegroundColor Yellow
    }
    else
    {
        Write-Host "Skipping image build (-NoBuildImage specified)." -ForegroundColor Yellow
    }


}


# Run the build within the container
if (-not $BuildImage)
{
    # Common setup for both Claude and normal build modes
    $pwshPath = if ($IsUnix) { '/usr/bin/pwsh' } else { 'C:\Program Files\PowerShell\7\pwsh.exe' }
    $initCall = if (-not $NoInit) { "& c:\docker-context\Init.g.ps1; " } else { "" }

    # Convert volume mappings to docker args format (interleave "-v" flags)
    $volumeArgs = @()
    foreach ($mapping in $VolumeMappings)
    {
        $volumeArgs += @("-v", $mapping)
    }

    if ($Claude)
    {
        # MCP server configuration
        $mcpPort = $null
        if (-not $NoMcp -and $mcpServerAvailable)
        {
            $mcpPort = $mcpFixedPort
        }
        elseif (-not $NoMcp)
        {
            Write-Host "Skipping MCP (server not running)." -ForegroundColor Yellow
        }
        else
        {
            Write-Host "Skipping MCP approval server (-NoMcp specified)." -ForegroundColor Yellow
        }

        # Run Claude mode
        Write-Host "Running Claude in the container." -ForegroundColor Green

        # Container will have its own Claude profile (no mount, no copy from host)
        $hostUserProfile = if ($IsUnix) { $env:HOME } else { $env:USERPROFILE }

        # Mount Claude sessions directory to preserve history (but not plugins)
        $hostClaudeSessions = Join-Path $hostUserProfile ".claude\.sessions"
        $containerClaudeSessions = Join-Path $containerUserProfile ".claude\.sessions"
        if (-not (Test-Path $hostClaudeSessions))
        {
            New-Item -ItemType Directory -Path $hostClaudeSessions -Force | Out-Null
        }
        $volumeArgs += @("-v", "${hostClaudeSessions}:${containerClaudeSessions}")
        Write-Host "Mounting Claude sessions directory: $hostClaudeSessions" -ForegroundColor Cyan

        # Mount Claude projects directory to share session history between container instances
        $hostClaudeProjects = Join-Path $hostUserProfile ".claude\projects"
        $containerClaudeProjects = Join-Path $containerUserProfile ".claude\projects"
        if (-not (Test-Path $hostClaudeProjects))
        {
            New-Item -ItemType Directory -Path $hostClaudeProjects -Force | Out-Null
        }
        $volumeArgs += @("-v", "${hostClaudeProjects}:${containerClaudeProjects}")
        Write-Host "Mounting Claude projects directory: $hostClaudeProjects" -ForegroundColor Cyan

        # Extract Claude prompt from remaining arguments if present
        # Usage: -Claude for interactive, -Claude "prompt" for non-interactive
        $ClaudePrompt = $null
        if ($BuildArgs -and $BuildArgs.Count -gt 0 -and $BuildArgs[0] -and -not $BuildArgs[0].StartsWith('-'))
        {
            $ClaudePrompt = $BuildArgs[0]
        }

        # Build inline script: subst drives, copy claude.json, cd to source, run Claude
        if ($ClaudePrompt)
        {
            # Non-interactive mode with prompt - no -it flags
            $dockerArgs = @()
            $mcpArg = if ($mcpPort)
            {
                " -McpPort $mcpPort"
            }
            else
            {
                ""
            }
            $inlineScript = "${substCommandsInline}${initCall}cd '$SourceDirName'; & .\eng\RunClaude.ps1 -Prompt `"$ClaudePrompt`"$mcpArg"
        }
        else
        {
            # Interactive mode - requires TTY
            $dockerArgs = @("-it")
            $mcpArg = if ($mcpPort)
            {
                " -McpPort $mcpPort"
            }
            else
            {
                ""
            }
            $inlineScript = "${substCommandsInline}${initCall}cd '$SourceDirName'; & .\eng\RunClaude.ps1$mcpArg"
        }

        # Environment variables to pass to container
        # No MCP secret needed - server binds to localhost only
        $envArgs = @()

        # No pwshArgs for Claude mode
        $pwshArgs = $null
        # No MCP cleanup needed - server runs independently
        $needsMcpCleanup = $false
    }
    else
    {
        # Run standard build mode
        # Delete now and not in the container because it's much faster and lock error messages are more relevant.
        Write-Host "Running the script in the container." -ForegroundColor Green

        # Prepare Build.ps1 arguments
        if ($StartVsmon)
        {
            $BuildArgs = @("-StartVsmon") + $BuildArgs
        }

        if ($Interactive)
        {
            $pwshArgs = "-NoExit"
            $BuildArgs = @("-Interactive") + $BuildArgs
            $dockerArgs = @("-it")
            $pwshExitCommand = ""
        }
        else
        {
            $pwshArgs = "-NonInteractive"
            $dockerArgs = @()
            $pwshExitCommand = "exit `$LASTEXITCODE`;"
        }

        $buildArgsString = $BuildArgs -join " "

        # Build inline script: subst drives, run init, cd to source, run build
        # Get full script path (combine with container source dir if relative)
        if ([System.IO.Path]::IsPathRooted($Script))
        {
            $scriptFullPath = $Script
        }
        else
        {
            $scriptFullPath = Join-Path $ContainerSourceDir $Script
        }
        $scriptInvocation = "& '$scriptFullPath'"
        $inlineScript = "${substCommandsInline}${initCall}cd '$SourceDirName'; $scriptInvocation $buildArgsString; $pwshExitCommand"

        # No environment args for normal build
        $envArgs = @()
        $needsMcpCleanup = $false
    }

    # Common docker execution for both modes
    $dockerArgsAsString = $dockerArgs -join " "

    # Execute docker command
    if ($existingContainerId)
    {
        # Reuse existing container with docker exec
        Write-Host "Executing: ``docker exec $existingContainerId $dockerArgsAsString -w $ContainerCallingDir $ImageTag `"$pwshPath`" $pwshArgs -Command `"$inlineScript`"" -ForegroundColor Cyan
        docker exec $dockerArgs  -w $ContainerCallingDir $existingContainerId $pwshPath $pwshArgs -Command $inlineScript
    }
    else
    {
        # Start new container with docker run
        # Build docker command with proper argument handling (avoid empty strings)
        $dockerCmd = @('run', '--rm')

        # Only add --memory and --cpus when NOT using process isolation
        if ($Isolation -ne 'process') {
            if ($Memory) {
                $dockerCmd += "--memory=$Memory"
            }
            $dockerCmd += "--cpus=$Cpus"
        }

        if ($isolationArg) { $dockerCmd += $isolationArg }
        $dockerCmd += $dockerArgs
        $dockerCmd += $volumeArgs
        $dockerCmd += $envArgs
        if ($pwshArgs) {
            $dockerCmd += @('-w', $ContainerCallingDir, $ImageTag, $pwshPath, $pwshArgs, '-Command', $inlineScript)
        } else {
            $dockerCmd += @('-w', $ContainerCallingDir, $ImageTag, $pwshPath, '-Command', $inlineScript)
        }

        Write-Host "Executing: ``docker $($dockerCmd -join ' ')" -ForegroundColor Cyan
        & docker @dockerCmd
    }
    $dockerExitCode = $LASTEXITCODE

    # Check exit code
    if ($dockerExitCode -ne 0)
    {
        Write-Host "Container failed with exit code $dockerExitCode" -ForegroundColor Red
        exit $dockerExitCode
    }
}
else
{
    Write-Host "Skipping container run (BuildImage specified)." -ForegroundColor Yellow
}

# Stop timing and display results
$elapsed = $stopwatch.Elapsed
Write-Host ""
Write-Host "Total build time: $($elapsed.ToString('hh\:mm\:ss\.fff') )" -ForegroundColor Cyan
Write-Host "Build completed at: $( Get-Date -Format 'yyyy-MM-dd HH:mm:ss' )" -ForegroundColor Cyan
}
finally
{
    # Restore original location
    Pop-Location
}
