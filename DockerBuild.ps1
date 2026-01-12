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
    [string]$ImageName, # Image name (defaults to a name based on the directory).
    [string]$BuildAgentPath = $(if ($env:TEAMCITY_JRE) { Split-Path $env:TEAMCITY_JRE -Parent } else { 'C:\BuildAgent' }),
    [switch]$LoadEnvFromKeyVault, # Forces loading environment variables form the key vault.
    [switch]$StartVsmon, # Enable the remote debugger.
    [string]$Script = 'Build.ps1', # The build script to be executed inside Docker.
    [string]$Isolation = 'process', # Docker isolation mode (process or hyperv).
    [string]$Memory = '16g', # Docker memory limit.
    [int]$Cpus = [Environment]::ProcessorCount, # Docker CPU limit (defaults to host's CPU count).
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
            $nugetPackages = Join-Path $env:USERPROFILE ".nuget\packages"
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
    $jsonPath = Join-Path $dockerContextDirectory "env.g.json"

    # Ensure the directory exists
    if (-not (Test-Path $dockerContextDirectory))
    {
        New-Item -ItemType Directory -Path $dockerContextDirectory -Force | Out-Null
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
        $nugetPackages = Join-Path $env:USERPROFILE ".nuget\packages"
    }
    $claudeEnv["NUGET_PACKAGES"] = $nugetPackages

    # Convert to JSON and save
    $jsonPath = Join-Path $dockerContextDirectory "env.g.json"

    # Ensure the directory exists
    if (-not (Test-Path $dockerContextDirectory))
    {
        New-Item -ItemType Directory -Path $dockerContextDirectory -Force | Out-Null
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

# Function to prepare MCP server for execution by copying to temp directory
# This avoids file locking issues when running the MCP server
function Copy-McpServerToTemp
{
    param(
        [string]$SourceRootDir
    )

    # Find the BuildTools Debug directory
    $debugDir = Join-Path $SourceRootDir "$EngPath\src\bin\Debug"

    if (-not (Test-Path $debugDir))
    {
        throw "MCP server Debug directory not found: $debugDir. Please build the project first using: Build.ps1"
    }

    # Get the single subdirectory (e.g., net8.0, net9.0)
    $targetFrameworkDirs = Get-ChildItem -Path $debugDir -Directory

    if ($targetFrameworkDirs.Count -eq 0)
    {
        throw "No target framework directory found in $debugDir. Please build the project first using: Build.ps1"
    }

    if ($targetFrameworkDirs.Count -gt 1)
    {
        Write-Warning "Multiple target framework directories found in $debugDir"
        Write-Warning "Using the first one: $( $targetFrameworkDirs[0].Name )"
    }

    $targetFrameworkDir = $targetFrameworkDirs[0].FullName
    Write-Host "Found MCP server build directory: $targetFrameworkDir" -ForegroundColor Cyan

    # Find the executable (.exe) or library (.dll)
    $exeFiles = Get-ChildItem -Path $targetFrameworkDir -Filter "*.exe"
    $dllFiles = Get-ChildItem -Path $targetFrameworkDir -Filter "*.dll" | Where-Object { $_.Name -notlike "*.resources.dll" }

    $executableFile = $null
    if ($exeFiles.Count -gt 0)
    {
        $executableFile = $exeFiles[0]
    }
    elseif ($dllFiles.Count -gt 0)
    {
        # Prefer files with "Build" in the name
        $buildDll = $dllFiles | Where-Object { $_.Name -like "*Build*" } | Select-Object -First 1
        if ($buildDll)
        {
            $executableFile = $buildDll
        }
        else
        {
            $executableFile = $dllFiles[0]
        }
    }
    else
    {
        throw "No executable (.exe) or assembly (.dll) found in $targetFrameworkDir"
    }

    Write-Host "Found MCP server executable: $( $executableFile.Name )" -ForegroundColor Cyan

    # Create temporary directory using hash of source directory
    # This ensures the same repo always uses the same temp path (avoiding firewall prompts)
    # but different repos won't conflict
    $hashBytes = (New-Object -TypeName System.Security.Cryptography.SHA256Managed).ComputeHash([System.Text.Encoding]::UTF8.GetBytes($SourceRootDir))
    $directoryHash = [System.BitConverter]::ToString($hashBytes, 0, 4).Replace("-", "").ToLower()
    $tempDir = Join-Path $env:TEMP "mcp-server-$directoryHash"

    # Clean up old temp directory if it exists
    if (Test-Path $tempDir)
    {
        Remove-Item $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }

    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null
    Write-Host "Created temporary directory: $tempDir" -ForegroundColor Cyan

    # Copy the entire target framework directory to temp
    $tempTargetDir = Join-Path $tempDir $targetFrameworkDirs[0].Name
    Copy-Item -Path $targetFrameworkDir -Destination $tempTargetDir -Recurse -Force
    Write-Host "Copied MCP server files to temporary directory" -ForegroundColor Cyan

    # Return the path to the executable and the temp directory for cleanup
    $tempExecutable = Join-Path $tempTargetDir $executableFile.Name
    return @{
        ExecutablePath = $tempExecutable
        TempDirectory = $tempDir
        IsExe = $executableFile.Extension -eq ".exe"
    }
}

if ($env:RUNNING_IN_DOCKER)
{
    Write-Error "Already running in Docker."
    exit 1
}

# Generate ImageName from script directory if not provided
if ( [string]::IsNullOrEmpty($ImageName))
{
    # Get full path without drive name (e.g., "C:\src\Metalama.Compiler" becomes "src\Metalama.Compiler")
    $fullPath = $PSScriptRoot -replace '^[A-Za-z]:\\', ''
    # Sanitize path to valid Docker image name (lowercase alphanumeric and hyphens only)
    $ImageTag = $fullPath.ToLower() -replace '[^a-z0-9\-]', '-' -replace '-+', '-' -replace '^-|-$', ''
    # Ensure it doesn't start with a hyphen and has at least one character
    if ([string]::IsNullOrEmpty($ImageTag) -or $ImageTag -match '^-')
    {
        $ImageTag = "docker-build-image"
    }
    Write-Host "Generated image name from directory: $ImageTag" -ForegroundColor Cyan
}
else
{
    # Generate a hash of the repo directory tagging (4 bytes, 8 hex chars)
    $hashBytes = (New-Object -TypeName System.Security.Cryptography.SHA256Managed).ComputeHash([System.Text.Encoding]::UTF8.GetBytes($PSScriptRoot))
    $directoryHash = [System.BitConverter]::ToString($hashBytes, 0, 4).Replace("-", "").ToLower()
    $ImageTag = "$ImageName`:$directoryHash"
    Write-Host "Image will be tagged as: $ImageTag" -ForegroundColor Cyan
}

# Save MCP server files to temp directory BEFORE cleanup (for -Claude mode)
# This must happen before cleaning because cleanup removes all bin directories
$mcpServerSnapshot = $null
if ($Claude -and -not $NoMcp)
{
    try
    {
        Write-Host "Building MCP server before cleanup..." -ForegroundColor Cyan
        $mcpProjectPath = Join-Path $PSScriptRoot "$EngPath\src"

        # Build the MCP server project
        & dotnet build $mcpProjectPath --configuration Debug --nologo --verbosity quiet
        if ($LASTEXITCODE -ne 0)
        {
            throw "Failed to build MCP server project at $mcpProjectPath"
        }

        Write-Host "Saving MCP server files before cleanup..." -ForegroundColor Cyan
        $mcpServerSnapshot = Copy-McpServerToTemp -SourceRootDir $PSScriptRoot
        Write-Host "MCP server files saved to: $( $mcpServerSnapshot.TempDirectory )" -ForegroundColor Cyan
    }
    catch
    {
        Write-Host "WARNING: Could not save MCP server files: $_" -ForegroundColor Yellow
        Write-Host "MCP server will not be available." -ForegroundColor Yellow
        $mcpServerSnapshot = $null
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

# Get the source directory name from $PSScriptRoot
$SourceDirName = $PSScriptRoot

# Start timing the entire process except cleaning
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# Ensure docker context directory exists and contains at least one file
if (-not (Test-Path $dockerContextDirectory))
{
    Write-Error "Docker context directory '$dockerContextDirectory' does not exist."
    exit 1
}


# Prepare volume mappings (stored as mapping strings, "-v" flags added later)
$VolumeMappings = @("${SourceDirName}:${SourceDirName}")
$MountPoints = @($SourceDirName)
$GitDirectories = @($SourceDirName)

# Define static Git system directory for mapping. This used by Teamcity as an LFS parent repo.
$gitSystemDir = "$BuildAgentPath\system\git"

if (Test-Path $gitSystemDir)
{
    $VolumeMappings += "${gitSystemDir}:${gitSystemDir}:ro"
    $MountPoints += $gitSystemDir
}

# Mount the host NuGet cache in the container.
if (-not $NoNuGetCache)
{
    # Use NUGET_PACKAGES from environment or default to user profile
    $nugetCacheDir = $env:NUGET_PACKAGES
    if ( [string]::IsNullOrEmpty($nugetCacheDir))
    {
        $nugetCacheDir = Join-Path $env:USERPROFILE ".nuget\packages"
    }

    Write-Host "NuGet cache directory: $nugetCacheDir" -ForegroundColor Cyan
    if (-not (Test-Path $nugetCacheDir))
    {
        Write-Host "Creating NuGet cache directory on host: $nugetCacheDir"
        New-Item -ItemType Directory -Force -Path $nugetCacheDir | Out-Null
    }

    # Mount to the same path in the container (will be transformed by Get-ContainerPath later)
    $VolumeMappings += "${nugetCacheDir}:${nugetCacheDir}"
    $MountPoints += $nugetCacheDir
}

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
            $VolumeMappings += "${targetPath}:${targetPath}:ro"
            $MountPoints += $targetPath
            $GitDirectories += $targetPath
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
        $VolumeMappings += "${siblingPath}:${siblingPath}:ro"
        $MountPoints += $siblingPath
        $GitDirectories += $siblingPath
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
        $VolumeMappings += "${engDirPath}:${engDirPath}:ro"
        $MountPoints += $engDirPath
        $GitDirectories += $engDirPath
    }
}

# Execute auto-generated DockerMounts.g.ps1 script to add more directory mounts.
$dockerMountsScript = Join-Path $EngPath 'DockerMounts.g.ps1'
if (Test-Path $dockerMountsScript)
{
    Write-Host "Importing Docker mount points from $dockerMountsScript" -ForegroundColor Cyan
    . $dockerMountsScript
}
elseif (-not $env:IS_TEAMCITY_AGENT)
{
    Write-Error "DockerMounts.g.ps1 not found at '$dockerMountsScript'. Run './Build.ps1 prepare' or './Build.ps1 dependencies update' to generate it."
    exit 1
}

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

# Transform MountPoints, GitDirectories, and SourceDirName for the container
$MountPoints = $MountPoints | ForEach-Object { Get-ContainerPath $_ }
$GitDirectories = $GitDirectories | ForEach-Object { Get-ContainerPath $_ }
$ContainerSourceDir = Get-ContainerPath $SourceDirName

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
$substCommandsInline = ""
foreach ($letter in $driveLetters.Keys | Sort-Object)
{
    $substCommandsInline += "C:\Windows\System32\subst.exe ${letter}: C:\$letter; "
}
if ($driveLetters.Count -gt 0)
{
    Write-Host "Drive letter mappings for container: $( $driveLetters.Keys -join ', ' )" -ForegroundColor Cyan
}

# Create Init.g.ps1 with git configuration (safe.directory and user identity)
$initScript = Join-Path $dockerContextDirectory "Init.g.ps1"
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
        `$normalizedDir = (`$dir -replace '\\\\', '/').TrimEnd('/') + '/'
        git config --global --add safe.directory `$normalizedDir
    }
}

"@
$initScriptContent | Set-Content -Path $initScript -Encoding UTF8

$mountPointsAsString = $MountPoints -Join ";"
$gitDirectoriesAsString = $GitDirectories -Join ";"

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

    if ($Claude)
    {
        $Dockerfile = "Dockerfile.claude"
    }
    else
    {
        $Dockerfile = "Dockerfile"
    }


    Write-Host "Building the image with tag: $ImageTag" -ForegroundColor Green
    Get-Content -Raw $Dockerfile | docker build -t $ImageTag --build-arg MOUNTPOINTS="$mountPointsAsString" -f - $dockerContextDirectory
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
    if ($Claude)
    {
        # Start MCP approval server on host with dynamic port in new terminal tab
        $mcpPort = $null
        $mcpPortFile = $null
        $mcpSecret = $null
        $mcpTempDir = $null
        if (-not $NoMcp)
        {
            try
            {
                # Check if MCP server snapshot was saved before cleanup
                if (-not $mcpServerSnapshot)
                {
                    throw "MCP server files were not saved before cleanup. Cannot start MCP server."
                }

                Write-Host "Starting MCP approval server..." -ForegroundColor Green
                $mcpPortFile = Join-Path $env:TEMP "mcp-port-$([System.Guid]::NewGuid().ToString('N').Substring(0, 8) ).txt"

                # Generate 128-bit (16 byte) random secret for authentication
                $randomBytes = New-Object byte[] 16
                [Security.Cryptography.RandomNumberGenerator]::Fill($randomBytes)
                # Use hex encoding (alphanumeric only, URL-safe and command-line safe)
                $mcpSecret = [BitConverter]::ToString($randomBytes).Replace('-', '').ToLower()
                Write-Host "Generated MCP authentication secret" -ForegroundColor Cyan

                # Use the MCP server snapshot saved before cleanup
                $mcpServerInfo = $mcpServerSnapshot
                $mcpTempDir = $mcpServerInfo.TempDirectory

                # Build the command to run in the new tab
                if ($mcpServerInfo.IsExe)
                {
                    # Run executable directly
                    $mcpCommand = "& '$( $mcpServerInfo.ExecutablePath )' tools mcp-server --port-file '$mcpPortFile' --secret '$mcpSecret'"
                }
                else
                {
                    # Run DLL with dotnet
                    $mcpCommand = "dotnet '$( $mcpServerInfo.ExecutablePath )' tools mcp-server --port-file '$mcpPortFile' --secret '$mcpSecret'"
                }

                # Try Windows Terminal first (wt.exe), fall back to conhost
                $wtPath = Get-Command wt.exe -ErrorAction SilentlyContinue
                if ($wtPath)
                {
                    # Open new tab in current Windows Terminal window
                    # The -w 0 option targets the current window
                    # Use single argument string for proper escaping
                    $wtArgString = "-w 0 new-tab --title `"MCP Approval Server`" -- pwsh -NoExit -Command `"$mcpCommand`""
                    $mcpServerProcess = Start-Process -FilePath "wt.exe" -ArgumentList $wtArgString -PassThru
                }
                else
                {
                    # Fallback: start in new console window
                    $mcpServerProcess = Start-Process -FilePath "pwsh" `
                        -ArgumentList "-NoExit", "-Command", $mcpCommand `
                        -PassThru
                }

                # Wait for port file to be written (with timeout)
                $timeout = 30
                $elapsed = 0
                while (-not (Test-Path $mcpPortFile) -and $elapsed -lt $timeout)
                {
                    Start-Sleep -Milliseconds 500
                    $elapsed += 0.5
                }

                if (-not (Test-Path $mcpPortFile))
                {
                    throw "MCP server failed to start within $timeout seconds"
                }

                $mcpPort = (Get-Content $mcpPortFile -Raw).Trim()
                Write-Host "MCP approval server running on port $mcpPort" -ForegroundColor Cyan
            }
            catch
            {
                Write-Host "ERROR: Failed to start MCP approval server: $_" -ForegroundColor Red
                Write-Host "Continuing without MCP server support." -ForegroundColor Yellow

                # Clean up on error
                if ($mcpServerProcess -and !$mcpServerProcess.HasExited)
                {
                    Stop-Process -Id $mcpServerProcess.Id -Force -ErrorAction SilentlyContinue
                }
                if ($mcpTempDir -and (Test-Path $mcpTempDir))
                {
                    Remove-Item $mcpTempDir -Recurse -Force -ErrorAction SilentlyContinue
                }

                # Reset variables to continue without MCP
                $mcpPort = $null
                $mcpSecret = $null
                $mcpTempDir = $null
            }
        }
        else
        {
            Write-Host "Skipping MCP approval server (-NoMcp specified)." -ForegroundColor Yellow
        }

        # Run Claude mode
        Write-Host "Running Claude in the container." -ForegroundColor Green

        # Add Claude-specific volume mounts for auth and settings
        $hostUserProfile = $env:USERPROFILE
        $containerUserProfile = "C:\Users\ContainerUser"

        # Mount .claude directory (settings and credentials)
        if (Test-Path "$hostUserProfile\.claude")
        {
            $VolumeMappings += "${hostUserProfile}\.claude:${containerUserProfile}\.claude"
        }

        # Copy .claude.json to docker-context (cannot mount files on Windows Docker)
        # Also fix installMethod to match container's npm installation
        $claudeJsonSource = "$hostUserProfile\.claude.json"
        $claudeJsonDest = Join-Path $dockerContextDirectory "claude.json"
        $copyClaudeJsonScript = ""
        if (Test-Path $claudeJsonSource)
        {
            $claudeConfig = Get-Content $claudeJsonSource -Raw | ConvertFrom-Json
            # Change installMethod to npm since that's how Claude is installed in container
            if ($claudeConfig.installMethod)
            {
                $claudeConfig.installMethod = "npm"
            }
            $claudeConfig | ConvertTo-Json -Depth 10 | Set-Content $claudeJsonDest -Encoding UTF8
            # Will copy from mounted source dir to user profile in container
            $copyClaudeJsonScript = "Copy-Item '$ContainerSourceDir\eng\docker-context\claude.json' '$containerUserProfile\.claude.json' -Force; "
        }

        # Mount .cache\claude (cache)
        if (Test-Path "$hostUserProfile\.cache\claude")
        {
            $VolumeMappings += "${hostUserProfile}\.cache\claude:${containerUserProfile}\.cache\claude"
        }

        # Convert volume mappings to docker args format (interleave "-v" flags)
        $volumeArgs = @()
        foreach ($mapping in $VolumeMappings)
        {
            $volumeArgs += @("-v", $mapping)
        }
        $VolumeMappingsAsString = ($VolumeMappings | ForEach-Object { "-v $_" }) -join " "

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
            $inlineScript = "${substCommandsInline}& c:\Init.g.ps1; ${copyClaudeJsonScript}cd '$SourceDirName'; & .\eng\RunClaude.ps1 -Prompt `"$ClaudePrompt`"$mcpArg"
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
            $inlineScript = "${substCommandsInline}& c:\Init.g.ps1; ${copyClaudeJsonScript}cd '$SourceDirName'; & .\eng\RunClaude.ps1$mcpArg"
        }

        $dockerArgsAsString = $dockerArgs -join " "
        $pwshPath = 'C:\Program Files\PowerShell\7\pwsh.exe'

        # Set HOME/USERPROFILE so Claude finds its config in the mounted location
        $envArgs = @(
            "-e", "HOME=$containerUserProfile",
            "-e", "USERPROFILE=$containerUserProfile"
        )

        # Pass MCP secret to container if MCP server is running
        if ($mcpSecret)
        {
            $envArgs += @("-e", "MCP_APPROVAL_SERVER_TOKEN=$mcpSecret")
        }

        try
        {
            # Start new container with docker run
            Write-Host "Executing: docker run --rm --memory=$Memory --cpus=$Cpus --isolation=$Isolation $dockerArgsAsString $VolumeMappingsAsString -e HOME=$containerUserProfile -e USERPROFILE=$containerUserProfile -w $ContainerSourceDir $ImageTag `"$pwshPath`" -Command `"$inlineScript`"" -ForegroundColor Cyan
            docker run --rm --memory=$Memory --cpus=$Cpus --isolation=$Isolation $dockerArgs @volumeArgs @envArgs -w $ContainerSourceDir $ImageTag $pwshPath -Command $inlineScript
            $dockerExitCode = $LASTEXITCODE
        }
        finally
        {
            # Cleanup MCP server after container exits (only if it was started)
            if ($mcpPort)
            {
                Write-Host "Stopping MCP approval server..." -ForegroundColor Cyan

                # Find the process listening on the MCP port and kill it
                try
                {
                    # Find PID using netstat
                    $netstatOutput = netstat -ano | Select-String ":$mcpPort\s" | Select-Object -First 1
                    if ($netstatOutput)
                    {
                        $parts = $netstatOutput.Line.Trim() -split '\s+'
                        $mcpPid = $parts[-1]
                        if ($mcpPid -and $mcpPid -match '^\d+$')
                        {
                            Stop-Process -Id $mcpPid -Force -ErrorAction SilentlyContinue
                            Write-Host "Stopped MCP server process (PID: $mcpPid)" -ForegroundColor Cyan
                        }
                    }
                }
                catch
                {
                    Write-Host "Could not stop MCP server via port lookup: $_" -ForegroundColor Yellow
                }

                # Fallback: try to find by command line
                $mcpProcesses = Get-Process -Name pwsh, dotnet -ErrorAction SilentlyContinue |
                        Where-Object { $_.CommandLine -like "*mcp-server*" }

                foreach ($proc in $mcpProcesses)
                {
                    try
                    {
                        Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
                        Write-Host "Stopped MCP server process $( $proc.Id )" -ForegroundColor Cyan
                    }
                    catch
                    {
                        # Process may have already exited
                    }
                }
            }

            # Clean up port file
            if ($mcpPortFile -and (Test-Path $mcpPortFile))
            {
                Remove-Item $mcpPortFile -ErrorAction SilentlyContinue
            }

            # Clean up temporary MCP server directory
            if ($mcpTempDir -and (Test-Path $mcpTempDir))
            {
                Write-Host "Cleaning up temporary MCP server directory: $mcpTempDir" -ForegroundColor Cyan
                Remove-Item $mcpTempDir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }

        if ($dockerExitCode -ne 0)
        {
            Write-Host "Docker run (Claude) failed with exit code $dockerExitCode" -ForegroundColor Red
            exit $dockerExitCode
        }
    }
    else
    {
        # Run standard build mode
        # Delete now and not in the container because it's much faster and lock error messages are more relevant.
        Write-Host "Building the product in the container." -ForegroundColor Green

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

        # Convert volume mappings to docker args format (interleave "-v" flags)
        $volumeArgs = @()
        foreach ($mapping in $VolumeMappings)
        {
            $volumeArgs += @("-v", $mapping)
        }
        $VolumeMappingsAsString = ($VolumeMappings | ForEach-Object { "-v $_" }) -join " "
        $dockerArgsAsString = $dockerArgs -join " "

        # Build inline script: subst drives, run init, cd to source, run build
        $inlineScript = "${substCommandsInline}& c:\Init.g.ps1; cd '$SourceDirName'; & .\$Script $buildArgsString; $pwshExitCommand"

        $pwshPath = 'C:\Program Files\PowerShell\7\pwsh.exe'

        # Build docker command arguments
        if ($existingContainerId)
        {
            # Reuse existing container with docker exec
            Write-Host "Executing: ``docker exec $existingContainerId $dockerArgsAsString -w $ContainerSourceDir $ImageTag `"$pwshPath`" $pwshArgs -Command `"$inlineScript`"" -ForegroundColor Cyan
            docker exec $dockerArgs  -w $ContainerSourceDir $existingContainerId $pwshPath $pwshArgs -Command $inlineScript

        }
        else
        {
            # Start new container with docker run
            Write-Host "Executing: ``docker run --rm --memory=$Memory --cpus=$Cpus --isolation=$Isolation $dockerArgsAsString $VolumeMappingsAsString -w $ContainerSourceDir $ImageTag `"$pwshPath`" $pwshArgs -Command `"$inlineScript`"" -ForegroundColor Cyan
            docker run --rm --memory=$Memory --cpus=$Cpus --isolation=$Isolation $dockerArgs @volumeArgs -w $ContainerSourceDir $ImageTag $pwshPath $pwshArgs -Command $inlineScript
        }

        if ($LASTEXITCODE -ne 0)
        {
            Write-Host "Container failed with exit code $LASTEXITCODE" -ForegroundColor Red
            exit $LASTEXITCODE
        }
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
