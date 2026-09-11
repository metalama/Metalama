# The original of this file is in <PostSharp.Engineering>/src/PostSharp.Engineering.BuildTools/Resources/DockerBuild.ps1.
# You can generate this file using `./Build.ps1 generate-scripts`.
# Documentation: https://raw.githubusercontent.com/postsharp/PostSharp.Engineering/HEAD/doc/dockerbuild.md

<#
.SYNOPSIS
    Builds and runs a Docker container for building the product or running Claude CLI.

.DESCRIPTION
    Builds a Docker image from the repository's Dockerfile, then runs the build script
    (or Claude CLI) inside a container with the source tree and dependencies mounted.

    The script automatically:
    - Collects environment variables and generates Init.g.ps1 for container startup
    - Mounts the source directory, NuGet cache, source-dependencies, and sibling repos
    - Handles non-C: drive letters on Windows via subst
    - Supports registry image caching for faster CI builds

.PARAMETER Interactive
    Opens an interactive PowerShell session inside the container.

.PARAMETER BuildImage
    Only builds the Docker image without running the build.

.PARAMETER NoBuildImage
    Skips building the Docker image (assumes it already exists).

.PARAMETER Clean
    Performs cleanup of bin and obj directories before building.

.PARAMETER NoNuGetCache
    Does not mount the host NuGet cache in the container.

.PARAMETER KeepInit
    Does not regenerate Init.g.ps1 (keeps the existing one as-is).
    The existing Init.g.ps1 is still executed. Cannot be combined with -PostInit.

.PARAMETER PostInit
    Path to a script to execute at the end of Init.g.ps1.
    The build fails if the PostInit script returns a non-zero exit code.
    Cannot be combined with -KeepInit or -NoInit.

.PARAMETER Claude
    Runs Claude CLI instead of Build.ps1. Use -Claude for interactive mode,
    or pass a prompt as a trailing argument for non-interactive mode.

.PARAMETER NoMcp
    Do not connect to the MCP approval server (for -Claude mode).

.PARAMETER Update
    Force full timestamp update to invalidate Docker cache and force Claude/plugin updates.

.PARAMETER ImageName
    Docker image name. Defaults to a content-hash-based name.

.PARAMETER BuildAgentPath
    Path to build agent directory. Defaults based on platform.

.PARAMETER LoadEnvFromKeyVault
    Forces loading environment variables from the PostSharpBuildEnv key vault.

.PARAMETER StartVsmon
    Mounts and enables the Visual Studio remote debugger in the container.

.PARAMETER Script
    The build script to execute inside Docker. Defaults to 'Build.ps1'.

.PARAMETER Dockerfile
    Path to a custom Dockerfile. Defaults to Dockerfile or Dockerfile.claude based on -Claude.

.PARAMETER RegistryImage
    Use a pre-built image from a registry, skipping Dockerfile build entirely.

.PARAMETER NoRegistry
    Ignore DOCKER_REGISTRY, DOCKER_USERNAME and DOCKER_PASSWORD, and build every image locally under a local
    tag: no authentication, no pull of an ancestor image, no push of what is built. Use it on a host that
    cannot reach the registry, or cannot verify its certificate, so that the build proceeds without the layer
    reuse the registry would otherwise give it.

.PARAMETER NoInit
    Do not generate or call Init.g.ps1 (skips environment variables, git config, safe.directory, etc).

.PARAMETER Isolation
    Docker isolation mode: 'process' or 'hyperv'. Windows only; ignored on Linux and macOS.
    When not specified, defaults to 'hyperv' on Windows Desktop and 'process' on Windows Server.
    On Windows, -Memory and a static -Cpus only apply under hyperv isolation.

.PARAMETER Memory
    Docker memory limit (e.g., "8g"). Applied on Linux and macOS, and on Windows under
    hyperv isolation; Windows process isolation ignores it.
    Clamped to the memory that the Docker engine reports, so a default larger than the
    machine does not produce a limit the engine cannot honour. The MSBuild node count
    passed to the container as MAX_BUILD_PARALLELISM is derived from the result, at one
    node per 4 GB.
    Defaults to $env:BuildAgentMemory (an integer in GB) if set, otherwise 24g.

.PARAMETER Cpus
    Docker CPU limit. Use a positive integer for a static limit, or "dynamic" for
    automatic allocation that rebalances CPUs across all managed containers.
    A static limit is applied wherever -Memory is; "dynamic" applies under any isolation.
    Defaults to $env:BuildAgentCpus if set, otherwise the host processor count.

.PARAMETER Mount
    Additional directories to mount from the host (readonly by default, append :w for writable).
    Supports * and ** glob patterns.

.PARAMETER Env
    Additional environment variables to pass from host to container.
    Supports "NAME" (read from host) and "NAME=VALUE" (literal) forms.

.PARAMETER Ports
    Port mappings from host to container (e.g., "8080:80", "3000").

.PARAMETER Label
    Label to apply to the container for identification (e.g., for cleanup of orphaned build containers).
    The label is set as "postsharp.build=<value>" on the container.

.PARAMETER MaxImageSpace
    Budget for the Docker image store, in gigabytes. Before the image chain is built, if the image
    store exceeds this budget, unused images are removed oldest first until the store is back within
    the budget.
    The budget is compared to the size that `docker system df` reports for images, which counts a
    layer shared by several images only once.
    The removal covers every image on the Docker engine, not only the images of this repository.
    It never removes an image that this run needs, an image that any container references, or an
    image created in the last two hours.
    Gigabytes are decimal (1 GB = 1e9 bytes), which is the unit `docker system df` prints.
    Set it to 0 to disable the cleanup.
    Defaults to $env:DOCKER_MAX_IMAGE_SPACE if set, otherwise 100.

.PARAMETER BuildArgs
    Arguments passed to Build.ps1 within the container (or Claude prompt if -Claude is specified).

.EXAMPLE
    .\DockerBuild.ps1 build
    Builds the image and runs Build.ps1 inside the container.

.EXAMPLE
    .\DockerBuild.ps1 -Claude
    Builds the image and starts an interactive Claude CLI session.

.EXAMPLE
    .\DockerBuild.ps1 -Claude "Fix the failing tests"
    Runs Claude CLI with the given prompt in non-interactive mode.

.EXAMPLE
    .\DockerBuild.ps1 -Interactive
    Opens an interactive PowerShell session inside the container.

.EXAMPLE
    .\DockerBuild.ps1 build -PostInit eng/SetupLocalDb.ps1
    Runs the build with a PostInit script that executes after Init.g.ps1.
#>

[CmdletBinding(PositionalBinding = $false)]
param(
    [switch]$Interactive, # Opens an interactive PowerShell session
    [switch]$BuildImage, # Only builds the image, but does not build the product.
    [switch]$NoBuildImage, # Does not build the image.
    [switch]$Clean, # Performs cleanup of bin and obj directories.
    [switch]$NoNuGetCache, # Does not mount the host nuget cache in the container.
    [switch]$KeepInit, # Does not regenerate Init.g.ps1 (keeps the existing one as-is).
    [string]$PostInit, # Script to execute at the end of Init.g.ps1 (fails the build if it fails).
    [switch]$Claude, # Run Claude CLI instead of Build.ps1. Use -Claude for interactive, -Claude "prompt" for non-interactive.
    [switch]$NoMcp, # Do not start the MCP approval server (for -Claude mode).
    [switch]$Update, # Force full timestamp update to invalidate Docker cache and force Claude/plugin updates.
    [string]$ImageName, # Image name (defaults to a name based on the directory).
    [string]$BuildAgentPath, # Path to build agent directory (defaults based on platform).
    [switch]$LoadEnvFromKeyVault, # Forces loading environment variables form the key vault.
    [switch]$StartVsmon, # Enable the remote debugger.
    [string]$Script = 'Build.ps1', # The build script to be executed inside Docker.
    [string]$Dockerfile, # Path to custom Dockerfile (defaults to Dockerfile or Dockerfile.claude based on -Claude).
    [string]$RegistryImage, # Use a pre-built image from a registry, skipping Dockerfile build entirely.
    [switch]$NoRegistry, # Ignore DOCKER_REGISTRY and its credentials; build locally without pulling or pushing.
    [switch]$NoInit, # Do not generate or call Init.g.ps1 (skips git config, safe.directory, etc).
    [string]$Isolation = 'process', # Docker isolation mode (process or hyperv). Windows only. When not specified, defaults to hyperv on Windows Desktop and process on Windows Server. Memory/CPU limits only apply to hyperv.
    [string]$Memory = $(if ($env:BuildAgentMemory) { "${env:BuildAgentMemory}g" } else { '24g' }), # Docker memory limit (e.g., "8g"). Applied except under Windows process isolation, and clamped to the memory reported by the Docker engine. Defaults to $env:BuildAgentMemory (in GB) or 24g.
    [string]$Cpus = $(if ($env:BuildAgentCpus) { $env:BuildAgentCpus } else { [Environment]::ProcessorCount }), # Docker CPU limit. Use a positive integer or "dynamic". Defaults to $env:BuildAgentCpus or host processor count.
    [string[]]$Mount, # Additional directories to mount from host (readonly by default, append :w for writable). Supports * and ** glob patterns.
    [string[]]$Env, # Additional environment variables to pass from host to container.
    [string[]]$Ports, # Port mappings from host to container (e.g., "8080:80", "3000").
    [string]$Label, # Label to apply to the container (e.g., for identifying build containers for cleanup).
    [string]$MaxImageSpace = $(if ($env:DOCKER_MAX_IMAGE_SPACE) { $env:DOCKER_MAX_IMAGE_SPACE } else { '100' }), # Budget for the Docker image store, in decimal GB. Unused images are removed oldest first before the build when the store exceeds it. 0 disables the cleanup. Defaults to $env:DOCKER_MAX_IMAGE_SPACE or 100.
    [Parameter(ValueFromRemainingArguments)]
    [string[]]$BuildArgs   # Arguments passed to `Build.ps1` within the container (or Claude prompt if -Claude is specified).
)

# Require PowerShell 7.5 or higher (run with pwsh, not powershell)
if ($PSVersionTable.PSVersion -lt [Version]'7.5')
{
    Write-Error "This script requires PowerShell 7.5 or higher (run with 'pwsh', not 'powershell'). Current version: $( $PSVersionTable.PSVersion )"
    exit 1
}


# A NuGet packages directory under C:\Windows\System32 is unsafe. 32-bit build tools restored there hit WOW64
# file-system redirection, which rewrites C:\Windows\System32 -> C:\Windows\SysWOW64 for a 32-bit process - so
# the tool's own image resolves to a non-existent SysWOW64 path and the CLR shim aborts with exit -2146232576
# (0x80131700, CLR_E_SHIM_RUNTIMELOAD). This happens silently when the agent service runs as SYSTEM (whose profile
# is under System32) and NUGET_PACKAGES is unset, so fail fast with the fix instead of a cryptic build failure.
function Assert-NuGetPackagesPathSafe([string]$path)
{
    if ($IsUnix -or [string]::IsNullOrEmpty($path)) { return }
    if (($path -replace '/', '\') -match '(?i)^[a-z]:\\windows\\system32(\\|$)')
    {
        Write-Host "NUGET_PACKAGES resolves to '$path', under C:\Windows\System32 - 32-bit build tools fail" -ForegroundColor Red
        Write-Host "there via WOW64 System32->SysWOW64 redirection (exit 0x80131700, CLR_E_SHIM_RUNTIMELOAD)." -ForegroundColor Red
        Write-Host "Set a machine-level NUGET_PACKAGES off System32, e.g.:" -ForegroundColor Red
        Write-Host "  md C:\packages; [Environment]::SetEnvironmentVariable('NUGET_PACKAGES','C:\packages','Machine')" -ForegroundColor Red
        exit 1
    }
}

####
# These settings are replaced by the generate-scripts command.
$EngPath = 'eng'
$EnvironmentVariables = 'AWS_ACCESS_KEY_ID,AWS_SECRET_ACCESS_KEY,AZ_IDENTITY_USERNAME,AZURE_CLIENT_ID,AZURE_CLIENT_SECRET,AZURE_DEVOPS_TOKEN,AZURE_DEVOPS_USER,AZURE_TENANT_ID,CLAUDE_CODE_OAUTH_TOKEN,DOC_API_KEY,DOWNLOADS_API_KEY,ENG_USERNAME,GIT_USER_EMAIL,GIT_USER_NAME,GITHUB_APP_ID,GITHUB_APP_PRIVATE_KEY,GITHUB_AUTHOR_EMAIL,GITHUB_REVIEWER_TOKEN,GITHUB_TOKEN,IS_POSTSHARP_OWNED,IS_TEAMCITY_AGENT,MetalamaLicense,NUGET_ORG_API_KEY,PostSharpLicense,SIGNSERVER_SECRET,TEAMCITY_CLOUD_TOKEN,TYPESENSE_API_KEY,VS_MARKETPLACE_ACCESS_TOKEN,VSS_NUGET_EXTERNAL_FEED_ENDPOINTS'
$DockerImagePrefix = 'metalama-2027.0'
$OvercommitRatio = 1.0
####

$ErrorActionPreference = "Stop"
$dockerContextDirectory = "$EngPath/docker-context"

# Detect platform (use built-in variables if available, fallback for older PowerShell)
if ($null -eq $IsWindows)
{
    $IsWindows = [System.Environment]::OSVersion.Platform -eq [System.PlatformID]::Win32NT
}
$IsUnix = -not $IsWindows  # Covers both Linux and macOS

# Docker isolation is Windows-only. Windows Server supports process isolation (faster,
# no per-container VM); Windows Desktop (client) only reliably runs hyperv isolation.
# Auto-detect by Windows edition unless -Isolation was passed explicitly.
if ($IsWindows -and -not $PSBoundParameters.ContainsKey('Isolation'))
{
    # Win32_OperatingSystem.ProductType: 1 = Workstation (Desktop), 2/3 = Server.
    $productType = (Get-CimInstance -ClassName Win32_OperatingSystem).ProductType
    $Isolation = if ($productType -eq 1) { 'hyperv' } else { 'process' }
    Write-Host "Detected Windows ProductType=$productType; using --isolation=$Isolation" -ForegroundColor Cyan
}
$isolationArg = if ($IsWindows)
{
    "--isolation=$Isolation"
}
else
{
    ""
}

# --memory and --cpus are honoured by the Linux and macOS engines whatever $Isolation says: isolation modes
# are a Windows concept and nothing outside $isolationArg acts on the value there. On Windows the limits only
# take effect under hyperv isolation - a process-isolated container shares the host kernel and the daemon
# silently drops both flags. Guarding on $Isolation alone would therefore leave every Linux container
# unlimited, which also loses MAX_BUILD_PARALLELISM (msbuild.ps1 derives the node count from the memory
# budget, and falls back to one node per CPU when there is none).
$supportsResourceLimits = $IsUnix -or $Isolation -ne 'process'

# Set BuildAgentPath default based on platform
if ( [string]::IsNullOrEmpty($BuildAgentPath))
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

# Resolve PostInit path relative to original current directory (before changing location)
if ($PostInit -and -not [System.IO.Path]::IsPathRooted($PostInit))
{
    $PostInit = Join-Path $CallingDirectory $PostInit
}

# Save current location and restore on exit
Push-Location
try
{
    Set-Location $PSScriptRoot

    # Validate parameter combinations
    if ($PostInit -and $NoInit)
    {
        Write-Error "-PostInit cannot be used with -NoInit."
        exit 1
    }
    if ($PostInit -and $KeepInit)
    {
        Write-Error "-PostInit cannot be used with -KeepInit."
        exit 1
    }

    # Validate and parse -Cpus parameter
    $isDynamicCpus = $false
    if ($Cpus -eq 'dynamic')
    {
        $isDynamicCpus = $true
        $TotalCpus = if ($env:BuildAgentCpus) { [int]$env:BuildAgentCpus } else { [Environment]::ProcessorCount }
        Write-Host "Dynamic CPU allocation enabled. Total CPUs: $TotalCpus, Overcommit ratio: $OvercommitRatio" -ForegroundColor Cyan
    }
    else
    {
        $cpuInt = 0
        if (-not [int]::TryParse($Cpus, [ref]$cpuInt) -or $cpuInt -le 0)
        {
            Write-Error "-Cpus must be a positive integer or 'dynamic'. Got: '$Cpus'"
            exit 1
        }
        $Cpus = $cpuInt
    }

    # Validate and parse -MaxImageSpace. An empty value or 0 disables the image-space cleanup; anything else
    # must be a whole number of gigabytes. An unparseable value is a hard error, as for -Cpus: this variable is
    # normally set once for a whole build agent, so a typo that silently disabled the cleanup would only be
    # discovered as a full disk, weeks later.
    $maxImageSpaceBytes = [long]0
    if (-not [string]::IsNullOrWhiteSpace($MaxImageSpace))
    {
        $maxImageSpaceGb = 0
        if (-not [int]::TryParse($MaxImageSpace.Trim(), [ref]$maxImageSpaceGb) -or $maxImageSpaceGb -lt 0)
        {
            Write-Error "-MaxImageSpace must be a whole number of gigabytes, or 0 to disable the image-space cleanup. Got: '$MaxImageSpace'"
            exit 1
        }

        # Docker prints sizes in DECIMAL gigabytes, so the budget uses the same unit as the number it is
        # compared to. PowerShell's 1GB is binary, and would silently turn a budget of 100 into 107.4 of the
        # gigabytes that `docker system df` reports.
        $maxImageSpaceBytes = [long]$maxImageSpaceGb * 1000000000
    }

    # Images created within this window are never removed, and `docker image prune` is given the same window.
    # This is what makes the cleanup safe against the concurrent DockerBuild runs that share a build agent. A
    # sibling run's freshly built or pulled chain image, and its boot image between `docker build` and
    # `docker run`, are referenced by no container, are absent from this run's keep set, and are invisible to
    # everything else here. Two hours is much longer than that window, and costs nothing on an agent whose
    # image store has grown over weeks.
    $ImageCleanupGraceHours = 2

    # How many measure-and-remove passes the cleanup makes. Each pass costs one `docker system df`, which is
    # the expensive part, and each pass necessarily removes too little, because the size reported for an image
    # includes the layers it shares with images that survive. A chain therefore loses one level per pass. The
    # chains here are three deep, so four passes leave one to spare, and whatever is not freed by then is freed
    # by the next build.
    $ImageCleanupMaxPasses = 4

    # Wall-clock budget for the whole cleanup. `docker system df` walks the layer store and can take minutes on
    # an agent that holds hundreds of images. Freeing disk must never become the slowest part of the build.
    $ImageCleanupTimeoutMinutes = 10

    # msbuild.ps1 budgets one MSBuild node per 4 GB of the container's memory.
    $MinMemoryPerCpuGb = 4

    # -Memory defaults to 24g, which is more than several agents have. A limit above what the engine can honour is
    # worse than no limit at all on Linux: the cgroup ceiling is then unreachable, so nothing constrains the build,
    # and the node count below would be derived from memory the container can never use. Ask the engine what it
    # actually has and clamp to it. A failure to reach the engine leaves the requested value untouched.
    $memoryGb = 0
    if ($supportsResourceLimits -and $Memory -match '^\s*(\d+(?:\.\d+)?)\s*([gm])b?\s*$')
    {
        $memoryGb = [double]$Matches[1]
        if ($Matches[2] -eq 'm') { $memoryGb = $memoryGb / 1024 }

        $engineMemoryBytes = [long]0
        $engineMemoryRaw = "$( docker info --format '{{.MemTotal}}' 2>$null )".Trim()
        if ([long]::TryParse($engineMemoryRaw, [ref]$engineMemoryBytes) -and $engineMemoryBytes -gt 0)
        {
            $engineMemoryGb = $engineMemoryBytes / 1GB
            if ($memoryGb -gt $engineMemoryGb)
            {
                $clampedGb = [int][Math]::Max(1, [Math]::Floor($engineMemoryGb))
                Write-Host "Requested --memory=$Memory exceeds the $( [Math]::Round($engineMemoryGb, 1) )g reported by the Docker engine; clamping to ${clampedGb}g" -ForegroundColor Yellow
                $Memory = "${clampedGb}g"
                $memoryGb = $clampedGb
            }
        }
    }

    # Derive the node count here, where the container's memory budget is known. Inside the container msbuild.ps1
    # cannot read the cgroup limit, so without this it falls back to the processor count and over-subscribes a
    # small agent - 16 nodes against 7 GB on the cell that reported this.
    $maxBuildParallelism = 0
    if ($memoryGb -gt 0)
    {
        $maxBuildParallelism = [int][Math]::Max(1, [Math]::Floor($memoryGb / $MinMemoryPerCpuGb))
    }

    if ($env:IS_TEAMCITY_AGENT)
    {
        Write-Host "Running on TeamCity agent at '$BuildAgentPath'" -ForegroundColor Cyan
    }

    # Dynamic CPU allocation helpers
    $DynamicCpuLabel = 'managed-by=DockerBuild'

    function Get-DynamicCpuAllocation
    {
        param(
            [int]$AdditionalContainers = 0
        )

        $budget = $TotalCpus * (1.0 + $OvercommitRatio)

        # Count running containers with the dynamic CPU label
        $containerIds = @(docker ps -q --filter "label=$DynamicCpuLabel" 2>$null)
        # Filter out empty strings from docker output
        $containerIds = @($containerIds | Where-Object { $_ -and $_.Trim() -ne '' })
        $runningCount = $containerIds.Count

        $totalContainers = $runningCount + $AdditionalContainers
        if ($totalContainers -le 0) { $totalContainers = 1 }

        $allocation = [Math]::Min($TotalCpus, [Math]::Floor($budget / $totalContainers))
        if ($allocation -lt 1) { $allocation = 1 }

        return @{
            Allocation   = [int]$allocation
            ContainerIds = $containerIds
        }
    }

    function Invoke-DynamicCpuRebalance
    {
        param(
            [int]$AdditionalContainers = 0
        )

        $result = Get-DynamicCpuAllocation -AdditionalContainers $AdditionalContainers
        $allocation = $result.Allocation
        $containerIds = $result.ContainerIds

        if ($containerIds.Count -gt 0)
        {
            Write-Host "Rebalancing $( $containerIds.Count ) managed container(s) to $allocation CPUs each" -ForegroundColor Cyan
            foreach ($cid in $containerIds)
            {
                try
                {
                    docker update --cpus=$allocation $cid 2>$null | Out-Null
                }
                catch
                {
                    Write-Warning "Failed to rebalance container $cid`: $_"
                }
            }
        }
        else
        {
            Write-Host "Dynamic CPU allocation: $allocation CPUs (no other managed containers)" -ForegroundColor Cyan
        }

        return $allocation
    }

    # Function to collect environment variables for container
    function New-EnvHashtable
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

        # Process additional environment variables from -Env parameter
        # Supports both "NAME" (read from host) and "NAME=VALUE" (literal value) forms
        if ($Env -and $Env.Count -gt 0)
        {
            foreach ($envSpec in $Env)
            {
                if ($envSpec -match '^([^=]+)=(.*)$')
                {
                    # NAME=VALUE form: use literal value
                    $envVarName = $Matches[1]
                    $value = $Matches[2]
                    $envVariables[$envVarName] = $value
                }
                else
                {
                    # NAME form: read from host environment
                    $envVarName = $envSpec
                    $value = [Environment]::GetEnvironmentVariable($envVarName)
                    if (-not [string]::IsNullOrEmpty($value))
                    {
                        $envVariables[$envVarName] = $value
                    }
                }
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
            Assert-NuGetPackagesPathSafe ($envVariables["NUGET_PACKAGES"])
        }

        # Add secrets from the PostSharpBuildEnv key vault, on our development machines.
        # On CI agents, these environment variables are supposed to be set by the host.
        # -BuildImage only builds the image; the secrets below are for the container run, so do not
        # require an Azure login in that case.
        if ($LoadEnvFromKeyVault -or ($env:IS_POSTSHARP_OWNED -and -not $env:IS_TEAMCITY_AGENT -and -not $BuildImage))
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

        # Print sorted list of environment variables being passed
        $sortedKeys = $envVariables.Keys | Sort-Object
        Write-Host "Environment variables: $( $sortedKeys -join ', ' )" -ForegroundColor Gray

        # Store in script-level variable for Init.g.ps1 generation
        $script:ContainerEnvironmentVariables = $envVariables
    }

    # Function to collect Claude-specific environment variables for container
    function New-ClaudeEnvHashtable
    {
        $claudeEnv = @{ }

        # Process $EnvironmentVariables list - only transfer variables that have CLAUDE_ prefix defined
        # e.g., if CLAUDE_GITHUB_TOKEN is set, transfer it as GITHUB_TOKEN
        $envVarNames = $EnvironmentVariables -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
        foreach ($envVarName in $envVarNames)
        {
            $claudeVarName = "CLAUDE_$envVarName"
            $value = [Environment]::GetEnvironmentVariable($claudeVarName)
            if (-not [string]::IsNullOrEmpty($value))
            {
                $claudeEnv[$envVarName] = $value
            }
        }

        # Preserved variables (transferred as-is, without requiring CLAUDE_ prefix)
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

        # Git identity - CLAUDE_ prefixed vars take precedence, then GIT_USER_*, then git config
        $gitUserName = $env:CLAUDE_GIT_USER_NAME
        if (-not $gitUserName)
        {
            $gitUserName = $env:GIT_USER_NAME
        }
        if (-not $gitUserName)
        {
            $gitUserName = git config --global user.name
        }
        $gitUserEmail = $env:CLAUDE_GIT_USER_EMAIL
        if (-not $gitUserEmail)
        {
            $gitUserEmail = $env:GIT_USER_EMAIL
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
        Assert-NuGetPackagesPathSafe ($claudeEnv["NUGET_PACKAGES"])

        # Process additional environment variables from -Env parameter
        # Supports both "NAME" (read from host) and "NAME=VALUE" (literal value) forms
        # In Claude mode, CLAUDE_FOO takes precedence over FOO
        if ($Env -and $Env.Count -gt 0)
        {
            foreach ($envSpec in $Env)
            {
                if ($envSpec -match '^([^=]+)=(.*)$')
                {
                    # NAME=VALUE form: use literal value
                    $envVarName = $Matches[1]
                    $value = $Matches[2]
                    $claudeEnv[$envVarName] = $value
                }
                else
                {
                    # NAME form: read from host environment (with CLAUDE_ prefix support)
                    $envVarName = $envSpec
                    $claudeVarName = "CLAUDE_$envVarName"
                    $value = [Environment]::GetEnvironmentVariable($claudeVarName)
                    if ( [string]::IsNullOrEmpty($value))
                    {
                        $value = [Environment]::GetEnvironmentVariable($envVarName)
                    }
                    if (-not [string]::IsNullOrEmpty($value))
                    {
                        $claudeEnv[$envVarName] = $value
                    }
                }
            }
        }

        # Print sorted list of environment variables being passed
        $sortedKeys = $claudeEnv.Keys | Sort-Object
        Write-Host "Environment variables: $( $sortedKeys -join ', ' )" -ForegroundColor Gray

        # Store in script-level variable for Init.g.ps1 generation
        $script:ContainerEnvironmentVariables = $claudeEnv
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
        # Persists $script:DayStamp (the single source of truth, also mixed
        # into the image tag by Get-ContentHash in Claude mode) to disk so
        # Dockerfile.claude can COPY it in and invalidate inner layers on
        # the same week boundary as the outer image tag.

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

        # Only rewrite the file if the content would actually change — avoids
        # bumping mtime on every run, which would pointlessly invalidate the
        # Docker COPY layer for the timestamp file.
        $needsUpdate = $true
        if (Test-Path $timestampFile)
        {
            $currentTimestamp = Get-Content $timestampFile -Raw -ErrorAction SilentlyContinue
            if ($currentTimestamp -eq $script:DayStamp)
            {
                $needsUpdate = $false
            }
        }

        if ($needsUpdate)
        {
            Set-Content -Path $timestampFile -Value $script:DayStamp -NoNewline -Force
            $label = if ($Update) { "forced" } else { "weekly" }
            Write-Host "Timestamp file updated ($label): $script:DayStamp" -ForegroundColor Cyan
        }

        return $timestampFile
    }

    function Get-ContentHash
    {
        param(
            [string]$DockerfilePath,
            [string]$ContextDirectory,
            [string]$DayStamp,  # non-empty => mix into hash (used in -Claude mode)
            [string]$ExtraInput  # folded in so a base-image (or OS) change invalidates this image's hash
        )

        $hashInput = Get-Content $DockerfilePath -Raw -ErrorAction SilentlyContinue
        if (-not $hashInput)
        {
            $hashInput = ""
        }

        # Add context files (excluding generated .g/ directory, which holds
        # per-invocation files like env.g.json and Init.g.ps1).
        # Sort with the invariant culture so the file order (and therefore the hash) is identical regardless of the
        # host's locale. The default Sort-Object uses the current culture, which can order non-ASCII names differently
        # on different machines and yield a different tag for identical content.
        $contextFiles = Get-ChildItem $ContextDirectory -Recurse -File -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -notmatch '[/\\]\.g[/\\]' } |
                Sort-Object -Property FullName -Culture ([System.Globalization.CultureInfo]::InvariantCulture)

        foreach ($file in $contextFiles)
        {
            $content = Get-Content $file.FullName -Raw -ErrorAction SilentlyContinue
            if ($content)
            {
                $hashInput += "`n--- $( $file.Name ) ---`n"
                $hashInput += $content
            }
        }

        # When a week stamp is supplied (Claude mode), rotate the image tag once
        # per UTC week so @latest npm installs of the Claude CLI and marketplace
        # plug-ins actually get refreshed. Same string as update.timestamp.
        if ($DayStamp)
        {
            $hashInput += "`n--- day-stamp ---`n$DayStamp"
        }

        # Fold the base/OS discriminator so a parent-image change (or a different WINDOWS_VERSION) yields a
        # different tag for this image and all its descendants.
        if ($ExtraInput)
        {
            $hashInput += "`n--- base ---`n$ExtraInput"
        }

        # Normalize line endings so the hash is identical whether files were checked out with LF (typical on a
        # dev machine) or CRLF (git autocrlf on CI). Otherwise the same Dockerfile yields a different tag on CI
        # than on dev, the registry cache never hits, and CI rebuilds the chain from scratch every time.
        $hashInput = $hashInput -replace "`r", ""

        $hashBytes = [System.Security.Cryptography.SHA256]::Create().ComputeHash(
                [System.Text.Encoding]::UTF8.GetBytes($hashInput)
        )
        # Use 8 bytes (16 hex chars) for uniqueness
        return [System.BitConverter]::ToString($hashBytes, 0, 8).Replace("-", "").ToLower()
    }

    # --- Image chain resolution ---------------------------------------------------------------------
    # A Dockerfile may declare its parent with `ARG BASE_IMAGE=<parent>.Dockerfile`. We resolve that to the
    # parent's content-hash tag (building or pulling it first), fold the parent tag into this image's hash, and
    # inject --build-arg BASE_IMAGE=<parentTag>. The image NAME is the Dockerfile stem (e.g.
    # <prefix>-build.Dockerfile -> image <prefix>-build), so the product/version prefix lives in the file name.
    $script:resolvedTags = @{ }

    function Get-DockerfileStem([string]$dfPath)
    {
        return [System.IO.Path]::GetFileNameWithoutExtension($dfPath)
    }

    # Per-image build context: docker-context/<stem>. There is deliberately NO fallback to the shared docker-context
    # root: stray machine-specific files living there (e.g. .credentials.json, claude.json) would otherwise be folded
    # into the image content hash and produce different tags on different machines for identical content. A missing
    # directory is treated as an empty context by Get-ContentHash, and Build-OneImage creates it before building.
    function Get-ContextDirFor([string]$dfPath)
    {
        return Join-Path $dockerContextDirectory (Get-DockerfileStem $dfPath)
    }

    # True when an image bakes the weekly cache-buster (`COPY .g/update.timestamp`). This is the single
    # discriminator for "this is a Claude leaf": it decides that the day stamp folds into the tag, that the
    # image is local-only, and that the timestamp file is staged into its context. It is derived from the
    # Dockerfile body rather than from its name, because the stem carries a product-defined prefix
    # (AdditionalDockerfile "agent" -> agent-claude.Dockerfile), so comparing the stem to "claude" silently
    # misses every prefixed leaf.
    function Test-BakesCacheBuster([string]$dfPath)
    {
        $body = Get-Content $dfPath -Raw -ErrorAction SilentlyContinue
        return [bool]($body -and $body -match 'update\.timestamp')
    }

    # Stage the cache-buster into the context of the image that actually declares the COPY, i.e.
    # docker-context/<stem>/.g/ - for whatever stem, prefixed or not. The .g/ directory is gitignored and is
    # excluded from Get-ContentHash, so writing here neither becomes tracked nor perturbs any image tag.
    function Copy-TimestampToContext([string]$dfPath)
    {
        if (-not (Test-BakesCacheBuster $dfPath))
        {
            return
        }

        # The run step (-NoBuildImage) skips the up-front timestamp creation, yet it still (re)builds the
        # local-only Claude leaf when the daemon does not already carry it, so materialize the file on demand.
        # Get-TimestampFile is idempotent and reads the same $script:DayStamp the tag was computed from.
        if (-not $script:TimestampFile)
        {
            $script:TimestampFile = Get-TimestampFile
        }

        $gDir = Join-Path (Get-ContextDirFor $dfPath) ".g"
        if (-not (Test-Path $gDir))
        {
            New-Item -ItemType Directory -Path $gDir -Force | Out-Null
        }

        Copy-Item -Path $script:TimestampFile -Destination (Join-Path $gDir "update.timestamp") -Force
        Write-Host "Staged cache-buster timestamp into the context of '$( Get-DockerfileStem $dfPath )'" -ForegroundColor Cyan
    }

    # Parse the parent Dockerfile from `ARG BASE_IMAGE=<parent>.Dockerfile`; $null if this is a chain root.
    function Get-BaseDockerfile([string]$dfPath)
    {
        foreach ($line in (Get-Content $dfPath -ErrorAction SilentlyContinue))
        {
            if ($line -match '^\s*ARG\s+BASE_IMAGE\s*=\s*(\S+\.Dockerfile)\s*$')
            {
                return (Join-Path (Split-Path $dfPath -Parent) $Matches[1])
            }
        }
        return $null
    }

    # Pure: compute the content-hash tag for a Dockerfile and (recursively) its ancestors. No docker calls.
    function Resolve-ImageTag([string]$dfPath)
    {
        $key = $dfPath.ToLower()
        if ($script:resolvedTags.ContainsKey($key)) { return $script:resolvedTags[$key] }

        $baseFold = $null
        $baseDf = Get-BaseDockerfile $dfPath
        if ($baseDf)
        {
            if (-not (Test-Path $baseDf)) { Write-Error "Base Dockerfile '$baseDf' referenced by '$dfPath' was not found."; exit 1 }
            # Fold only the base's CONTENT HASH (the part after the last ':'), never the full tag - so the child
            # hash is independent of the registry prefix and is identical in local and registry modes.
            $baseFold = ((Resolve-ImageTag $baseDf) -split ':')[-1]
        }

        # OS discriminator so ltsc2025 / ltsc2022 produce distinct tags of the same image name. Propagates to
        # descendants through $baseFold.
        $extra = "os=$windowsVersion|base=$baseFold"

        # Fold the weekly stamp only for images that bake the update.timestamp cache-buster (the Claude leaf), so
        # @latest npm installs of the Claude CLI and plug-ins refresh once per UTC week.
        $hashDayStamp = if (Test-BakesCacheBuster $dfPath) { $script:DayStamp } else { $null }

        $hash = Get-ContentHash -DockerfilePath $dfPath -ContextDirectory (Get-ContextDirFor $dfPath) -DayStamp $hashDayStamp -ExtraInput $extra
        # The image NAME carries the product/version prefix ($DockerImagePrefix); the Dockerfile file stem does
        # not. e.g. stem 'build' -> image '<prefix>-build'. ARG BASE_IMAGE references stems (prefix-free).
        $imageName = "$DockerImagePrefix-$( Get-DockerfileStem $dfPath )"
        $tag = if ($dockerRegistry) { "${dockerRegistry}/${imageName}:${hash}" } else { "${imageName}:${hash}" }
        $script:resolvedTags[$key] = $tag
        return $tag
    }

    # The platform-specific mountpoints-creation step. This is NEVER baked into a chain Dockerfile - it goes
    # only into the dynamically generated boot image (see New-BootImage), so the chain images stay clean and
    # free of the machine-specific mount set.
    function Get-MountpointsBlock
    {
        if ($IsWindows)
        {
            return @"
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
            return @"
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
    }

    # Parse the OS image a chain ROOT is built FROM, as declared by `ARG OS_IMAGE_REPOSITORY=` (the Windows
    # default root, which takes its tag from WINDOWS_VERSION) or `ARG OS_IMAGE=` (every other root, which
    # declares a complete reference). Returns $null for a Dockerfile that declares neither, which is every
    # image that is not a chain root.
    #
    # Pure: it only reads the file. That is what lets the image-space cleanup protect the OS image before any
    # image is built, without asking the daemon anything. ArgName tells the caller which build-arg the value
    # belongs to, because the two spellings need different values (a repository, or a full reference).
    function Get-OsImageSpec([string]$dfPath)
    {
        $content = Get-Content -Raw $dfPath -ErrorAction SilentlyContinue
        if (-not $content) { return $null }

        if ($windowsVersion -and ($content -match 'ARG\s+OS_IMAGE_REPOSITORY=(\S+)'))
        {
            return [pscustomobject]@{ Repository = $Matches[1]; Tag = $windowsVersion; ArgName = 'OS_IMAGE_REPOSITORY' }
        }

        if ($content -match 'ARG\s+OS_IMAGE=(\S+)')
        {
            # Split the trailing tag off the reference; a ':' before the last '/' belongs to a registry port.
            $ref = $Matches[1]
            $slash = $ref.LastIndexOf('/')
            $colon = $ref.LastIndexOf(':')
            if ($colon -gt $slash) { return [pscustomobject]@{ Repository = $ref.Substring(0, $colon); Tag = $ref.Substring($colon + 1); ArgName = 'OS_IMAGE' } }

            return [pscustomobject]@{ Repository = $ref; Tag = 'latest'; ArgName = 'OS_IMAGE' }
        }

        return $null
    }

    # Build one chain image from its STATIC Dockerfile, unmodified (per-image context, base build-arg).
    function Build-OneImage([string]$dfPath, [string]$tag, [string[]]$baseBuildArg)
    {
        $content = Get-Content -Raw $dfPath   # piped to docker build verbatim - the file on disk is never changed
        $ctxDir = Get-ContextDirFor $dfPath
        # The per-image context dir is normally created by generate-scripts, but it is not tracked by git (it is often
        # empty), so ensure it exists here before handing it to docker build.
        if (-not (Test-Path $ctxDir)) { New-Item -ItemType Directory -Path $ctxDir -Force | Out-Null }
        # Staged here, against the Dockerfile actually being built, so every Claude leaf gets the cache-buster in
        # its own context regardless of prefix, and images that do not bake it are left untouched.
        Copy-TimestampToContext $dfPath
        $cmd = @('build', '-t', $tag)
        if ($isolationArg) { $cmd += $isolationArg }
        if ($Memory -and $supportsResourceLimits) { $cmd += "--memory=$Memory" }
        # Pass WINDOWS_VERSION only to the root image that declares it (avoids 'unconsumed build-arg' warnings).
        if ($IsWindows -and $windowsVersion -and ($content -match 'ARG\s+WINDOWS_VERSION'))
        {
            $cmd += @('--build-arg', "WINDOWS_VERSION=$windowsVersion")
        }

        # Same for the OS image: only a root image declares it, and resolving it here (rather than up front)
        # means the mirror is only consulted when a root image is genuinely being built. The Windows default
        # root takes its tag from WINDOWS_VERSION and so declares a repository; every other root (Linux, and
        # any product that pins its own base image) declares a complete reference.
        $osImageSpec = Get-OsImageSpec $dfPath
        if ($osImageSpec)
        {
            $osImage = Get-OsImage $osImageSpec.Repository $osImageSpec.Tag
            $osImageValue = if ($osImageSpec.ArgName -eq 'OS_IMAGE_REPOSITORY') { $osImage.Repository } else { $osImage.Reference }
            $cmd += @('--build-arg', "$( $osImageSpec.ArgName )=$osImageValue")
        }
        $cmd += $baseBuildArg
        $cmd += @('-f', '-', $ctxDir)
        Write-Host "Building $tag" -ForegroundColor Green
        Write-Host "Docker command: docker $( $cmd -join ' ' )" -ForegroundColor Cyan
        # Pipe docker output to the host so it does NOT become this function's return value (which would
        # otherwise pollute the tag string the caller folds into the next --build-arg BASE_IMAGE).
        #
        # The config dir must be passed here too: `docker build` resolves the FROM itself, and when that is the
        # OS mirror (or any other image in the private registry) it needs the credentials that the login wrote
        # into the temporary config. Without it the build fails with "no basic auth credentials" on any agent
        # whose default config is not already logged in.
        $content | & docker @dockerConfigArg @cmd 2>&1 | Out-Host
        if ($LASTEXITCODE -ne 0) { Write-Host "Docker build failed for $tag (exit $LASTEXITCODE)" -ForegroundColor Red; exit $LASTEXITCODE }
        $script:builtNewImage = $true
    }

    # Build the local "boot" image: a thin layer over the resolved chain image that creates the bind-mount
    # directories. The mount set is machine-specific, so this is kept out of the shared chain images and is
    # never pushed. Returns the boot image tag, which is what `docker run` uses.
    #
    # The boot image is the leaf that `docker run` actually executes, so its tag must be GLOBALLY UNIQUE:
    # concurrent invocations on the same host resolve to the same chain hash and would otherwise collide on a
    # single boot tag, with one run rebuilding (or removing) the image out from under the other. A
    # YYYYMMDDTHHmmss timestamp suffix keeps each run's leaf image distinct. The image is removed after the run
    # (see the boot-image cleanup near the end), so unique tags do not accumulate.
    function New-BootImage([string]$baseTag)
    {
        $ref = ($baseTag -split '/')[-1]   # strip any registry prefix - the boot image is local only
        $stamp = (Get-Date).ToString("yyyyMMdd'T'HHmmss")   # local time; only needs to be unique per host run
        if ($ref -match '^(.*):([^:]+)$') { $bootTag = "$( $Matches[1] )-boot:$( $Matches[2] )-$stamp" } else { $bootTag = "$ref-boot:$stamp" }
        $script:BootImageTag = $bootTag   # tracked so the run can remove this leaf image afterwards

        # On Windows the mountpoints RUN uses backtick line-continuations, so set `# escape=` + backtick. On
        # Unix the block uses backslash continuations, so keep Docker's default escape char (emit no directive).
        $escapeLine = if ($IsWindows) { "# escape=$([char]96)`n" } else { "" }
        $content = $escapeLine + "FROM $baseTag`n" + (Get-MountpointsBlock)

        # The boot layer has no COPY, so build it against an empty context.
        $bootCtx = Join-Path ([System.IO.Path]::GetTempPath()) "docker-boot-$( New-Guid )"
        New-Item -ItemType Directory -Path $bootCtx -Force | Out-Null
        try
        {
            $cmd = @('build', '-t', $bootTag)
            if ($isolationArg) { $cmd += $isolationArg }
            if ($Memory -and $supportsResourceLimits) { $cmd += "--memory=$Memory" }
            $cmd += @('--build-arg', "MOUNTPOINTS=$mountPointsAsString", '-f', '-', $bootCtx)
            Write-Host "Building boot image $bootTag (bind-mount dirs) over $baseTag" -ForegroundColor Green
            # Its FROM is the local chain leaf, so no credentials are needed - but the config dir is passed for
            # consistency with Build-OneImage, and costs nothing when it is empty.
            $content | & docker @dockerConfigArg @cmd 2>&1 | Out-Host
            if ($LASTEXITCODE -ne 0) { Write-Host "Boot image build failed for $bootTag (exit $LASTEXITCODE)" -ForegroundColor Red; exit $LASTEXITCODE }
        }
        finally { Remove-Item $bootCtx -Recurse -Force -ErrorAction SilentlyContinue }
        return $bootTag
    }

    # Push one image to the registry in a background job, without waiting for it. The jobs are collected in
    # $script:RegistryPushJobs and waited for at the end of the script, where a failed push fails the build.
    function Start-AsyncPush([string]$tag)
    {
        if (-not $script:PushedTags.Add($tag))
        {
            return
        }

        Write-Host "  starting async push to registry" -ForegroundColor Cyan

        # Copied to a local so that $using: captures it: $dockerConfigArg belongs to the enclosing scope.
        $configArg = $dockerConfigArg

        $pushJob = Start-Job -ScriptBlock {
            docker @using:configArg push $using:tag 2>&1
            $LASTEXITCODE
        }

        $script:RegistryPushJobs += [pscustomobject]@{ Tag = $tag; Job = $pushJob }
    }

    # Wait for ALL async registry push jobs to complete (each image pushed in its own job). A push that failed
    # or timed out fails the script: an image missing from the registry is silently rebuilt from scratch by
    # every later build, which costs far more than a red build here.
    #
    # Called on the normal path and again from the `finally` block, so that the failure of a later step never
    # abandons a push in flight: the jobs die with the process, and a half-pushed image never becomes a tag in
    # the registry. The job list is emptied here, so the second call is a no-op after a normal completion.
    function Wait-ForRegistryPushes
    {
        if ($script:RegistryPushJobs.Count -eq 0)
        {
            return
        }

        Write-Host ""
        Write-Host "Waiting for $( $script:RegistryPushJobs.Count ) registry push job(s) to complete..." -ForegroundColor Cyan

        foreach ($entry in $script:RegistryPushJobs)
        {
            $completed = Wait-Job -Job $entry.Job -Timeout 1800  # 30 minute timeout per job
            if ($completed)
            {
                $jobOutput = Receive-Job -Job $entry.Job
                $exitCode = $jobOutput[-1]  # last item is the exit code
                $output = if ($jobOutput.Count -gt 1) { $jobOutput[0..($jobOutput.Count - 2)] -join "`n" } else { "" }

                if ($exitCode -eq 0)
                {
                    Write-Host "Registry push completed: $( $entry.Tag )" -ForegroundColor Green
                }
                else
                {
                    Write-Host "Registry push FAILED (exit $exitCode): $( $entry.Tag )" -ForegroundColor Red
                    if ($output) { Write-Host "Push output: $output" -ForegroundColor Gray }
                    $script:PushFailed = $true
                }
            }
            else
            {
                Write-Host "Registry push TIMED OUT after 30 minutes: $( $entry.Tag )" -ForegroundColor Red
                Stop-Job -Job $entry.Job
                $script:PushFailed = $true
            }
            Remove-Job -Job $entry.Job -Force
        }

        $script:RegistryPushJobs = @()
    }

    # The processor architecture of the Docker engine, in the naming Docker itself uses ('amd64', 'arm64').
    # It is the architecture of the OS image a mirror ends up holding, because `docker pull` of a
    # multi-architecture manifest selects the engine's own platform.
    #
    # The engine is asked rather than the host, because the two differ on macOS: the engine runs in a Linux
    # virtual machine (Docker Desktop, Colima, Rancher Desktop), and the images are of the virtual machine's
    # architecture, not of the architecture the PowerShell process happens to run under. Queried once per run.
    function Get-DockerArchitecture
    {
        if ($script:DockerArchitecture)
        {
            return $script:DockerArchitecture
        }

        # '{{.Server.Arch}}' is the daemon's own Go architecture name, the same vocabulary an image manifest
        # uses. `docker info --format '{{.Architecture}}'` is not interchangeable with it: that one reports the
        # uname form, 'x86_64' rather than 'amd64', which is why the result is normalized below.
        $architecture = (docker version --format '{{.Server.Arch}}' 2>$null | Select-Object -Last 1)

        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($architecture))
        {
            # The daemon could not be asked: it is not running, or on Linux the user is not in the docker
            # group. The host architecture is the next best answer, and is the right one wherever the daemon
            # runs on the host itself, which covers Linux and Windows.
            $architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
            Write-Host "Could not read the Docker engine architecture; assuming the host's, $architecture." -ForegroundColor Yellow
        }

        $architecture = $architecture.Trim()

        # Accept every spelling the two sources above can produce, including the .NET enum names ('X64',
        # 'Arm64') that the fallback returns.
        $script:DockerArchitecture = switch -Regex ($architecture)
        {
            '^(amd64|x64|x86_64)$' { 'amd64' }
            '^(arm64|aarch64)$' { 'arm64' }
            default { $architecture.ToLowerInvariant() }
        }

        return $script:DockerArchitecture
    }

    # Resolve the OS image a root image is built FROM, mirroring it into the configured registry the first time
    # it is needed. On a fresh agent the OS image is the most expensive download of the whole build, and the
    # registry is on the LAN, so building from the mirror replaces an internet transfer with a local one.
    # Agents that configure no registry (the cloud ones) go straight to the upstream registry, as before.
    #
    # The repository the given upstream OS repository is mirrored under in $dockerRegistry. Split out of
    # Get-OsImage so that the image-space cleanup can name the mirror without pulling or creating it.
    function Get-OsMirrorRepository([string]$repository)
    {
        # Flatten the upstream repository into a single product-neutral name, dropping the registry host:
        # 'mcr.microsoft.com/windows/servercore' -> 'windows-servercore', 'ubuntu' -> 'ubuntu'. Every product
        # using this registry then shares the one mirror.
        $segments = $repository -split '/'
        if ($segments.Length -gt 1 -and ($segments[0] -match '[.:]'))
        {
            $segments = $segments[1..($segments.Length - 1)]
        }

        # A repository holds one manifest per tag, and the mirror is single-platform: whichever agent creates
        # it decides the architecture every later agent gets, with no error until a command runs in a
        # container built from it. So the architecture belongs in the mirror name. amd64 keeps the unsuffixed
        # name, which every mirror already in the registry was pushed under:
        #   amd64 -> <registry>/ubuntu, arm64 -> <registry>/ubuntu-arm64.
        $architecture = Get-DockerArchitecture
        $architectureSuffix = if ($architecture -eq 'amd64')
        {
            ''
        }
        else
        {
            "-$architecture"
        }

        return "$dockerRegistry/$( $segments -join '-' )$architectureSuffix"
    }

    # Takes the upstream repository and tag, and returns an object with the Repository and the full Reference to
    # build from - either the mirror or, when there is no registry (or the mirror cannot be created), upstream.
    # Called only when a ROOT image is actually being built, so a run that pulls its whole chain never touches
    # the mirror. Each distinct image is resolved once per run.
    function Get-OsImage([string]$repository, [string]$tag)
    {
        $upstreamRef = "${repository}:${tag}"

        if ($script:OsImages.ContainsKey($upstreamRef))
        {
            return $script:OsImages[$upstreamRef]
        }

        $upstream = [pscustomobject]@{ Repository = $repository; Reference = $upstreamRef }

        if (-not $dockerRegistry)
        {
            $script:OsImages[$upstreamRef] = $upstream
            return $upstream
        }

        $mirrorRepository = Get-OsMirrorRepository $repository
        $mirror = [pscustomobject]@{ Repository = $mirrorRepository; Reference = "${mirrorRepository}:${tag}" }

        docker @dockerConfigArg manifest inspect $mirror.Reference *> $null
        if ($LASTEXITCODE -eq 0)
        {
            # Pull it here, with the credentials from the temporary config, rather than leaving it to the FROM
            # resolution inside `docker build`: the build only sees the credentials this script passes it, and
            # an agent whose default config is not logged in would otherwise fail with "no basic auth
            # credentials". Pulling it first also means the build never touches the registry at all.
            Write-Host "Building from the OS image mirrored at $( $mirror.Reference )" -ForegroundColor Green
            docker @dockerConfigArg pull $mirror.Reference 2>&1 | Out-Host

            if ($LASTEXITCODE -ne 0)
            {
                # The mirror is unusable on this agent; upstream still is. Not fatal.
                Write-Host "Could not pull the mirrored OS image; building from $upstreamRef." -ForegroundColor Yellow
                $script:OsImages[$upstreamRef] = $upstream
                return $upstream
            }

            $script:OsImages[$upstreamRef] = $mirror
            return $mirror
        }

        # Not mirrored yet: take it from upstream this once and mirror it, so that every later build on every
        # agent gets it from the registry. The push is normally near-instant even though the image is gigabytes:
        # those exact blobs already underlie every chain image pushed so far, so the registry mounts them across
        # repositories instead of receiving them again.
        Write-Host "The OS image is not mirrored yet; pulling $upstreamRef to mirror it" -ForegroundColor Cyan
        docker pull $upstreamRef 2>&1 | Out-Host

        if ($LASTEXITCODE -ne 0)
        {
            # Not fatal: the build below pulls the same image from upstream anyway, and reports its own error.
            Write-Host "Could not pull the OS image; building from $upstreamRef." -ForegroundColor Yellow
            $script:OsImages[$upstreamRef] = $upstream
            return $upstream
        }

        docker tag $upstreamRef $mirror.Reference | Out-Null
        Start-AsyncPush $mirror.Reference
        $script:OsImages[$upstreamRef] = $mirror

        return $mirror
    }

    # Ensure the image and its ancestors exist (parent first): use local, else pull, else build; start a push
    # when building in registry mode. Returns the image tag.
    # True when $tag exists in the registry. A miss is the ordinary case (the image has not been pushed yet)
    # and stays quiet, but any OTHER failure - experimental CLI gating, an untrusted certificate, a lost
    # session, an unreachable host - is reported once. Silencing those made a broken registry look exactly like
    # a cache miss, so every agent rebuilt the whole ancestor chain on every run and nobody could see why.
    function Test-ImageInRegistry([string]$tag)
    {
        $output = (docker @dockerConfigArg manifest inspect $tag 2>&1 | Out-String).Trim()

        if ($LASTEXITCODE -eq 0)
        {
            return $true
        }

        # A genuine "not in the registry" answer. Anything else is a configuration or connectivity fault.
        if ($output -notmatch 'manifest unknown|no such manifest|not found|manifest for .* not found')
        {
            if (-not $script:RegistryProbeWarned)
            {
                $script:RegistryProbeWarned = $true
                Write-Host "Warning: cannot query the registry for '$tag', so cached images cannot be reused and every layer will be rebuilt locally. $output" -ForegroundColor Yellow
            }
        }

        return $false
    }

    function Ensure-Image([string]$dfPath)
    {
        $baseBuildArg = @()
        $baseDf = Get-BaseDockerfile $dfPath
        if ($baseDf)
        {
            $baseTag = Ensure-Image $baseDf
            $baseBuildArg = @('--build-arg', "BASE_IMAGE=$baseTag")
        }

        $tag = Resolve-ImageTag $dfPath

        # The Claude leaf is ALWAYS built locally and is NEVER pulled from or pushed to the registry. It bakes a
        # weekly cache-buster (update.timestamp) and `@latest` npm/plugin installs, so a registry copy is stale by
        # design and sharing it saves nothing. Keeping it local-only also means a missing/unauthenticated registry
        # (which only ever served the stable ancestor chain) can never fail a Claude run on pull/push.
        $isClaudeLeaf = Test-BakesCacheBuster $dfPath

        Write-Host "Ensuring image: $tag" -ForegroundColor Cyan

        docker image inspect $tag *> $null
        if ($LASTEXITCODE -eq 0)
        {
            Write-Host "  found locally" -ForegroundColor Green
        }
        elseif (-not $isClaudeLeaf -and $dockerRegistry -and (Test-ImageInRegistry $tag))
        {
            Write-Host "  pulling from registry" -ForegroundColor Green
            docker @dockerConfigArg pull $tag 2>&1 | Out-Host
            if ($LASTEXITCODE -ne 0) { Write-Host "Docker pull failed for $tag" -ForegroundColor Red; exit 1 }
            return $tag
        }
        else
        {
            Build-OneImage $dfPath $tag $baseBuildArg
        }

        # Push the image as soon as it exists, if it isn't already in the registry: the push then overlaps the
        # builds of the images above it in the chain instead of waiting for all of them. The job is waited for
        # at the end of the script. The Claude leaf is excluded (see $isClaudeLeaf above): it is local-only and
        # never enters the registry.
        if ($dockerRegistry -and -not $isClaudeLeaf)
        {
            if (-not (Test-ImageInRegistry $tag))
            {
                Start-AsyncPush $tag
            }
        }
        return $tag
    }

    # Docker prints sizes with go-units: "0B", "45.5kB", "12.34GB", "1.1TB". `system df`, `image ls` and
    # `image prune` use the DECIMAL scale (kB = 1000); other Docker surfaces use the binary spellings ("1.1GiB").
    # Both are accepted, rather than guessing which one produced a given string.
    #
    # Returns -1, not 0, for anything that is not a size. Every caller has to tell "Docker reported zero" from
    # "Docker reported something this script does not understand", because the second one must disable the
    # cleanup instead of making it believe the image store is empty.
    function ConvertFrom-DockerSize([string]$text)
    {
        if ("$text" -notmatch '^\s*(\d+(?:\.\d+)?)\s*([kKmMgGtTpP]?)(i?)B\s*$')
        {
            return [long]-1
        }

        $exponent = switch ($Matches[2].ToUpperInvariant())
        {
            'K' { 1 }
            'M' { 2 }
            'G' { 3 }
            'T' { 4 }
            'P' { 5 }
            default { 0 }
        }

        return [long]([double]$Matches[1] * [Math]::Pow($( if ($Matches[3]) { 1024 } else { 1000 } ), $exponent))
    }

    # Formats a byte count in the DECIMAL gigabytes Docker prints (1 GB = 1e9 bytes), not in PowerShell's binary
    # 1GB, so that every line logged here agrees with the tool it quotes.
    function Format-Gigabytes([long]$bytes)
    {
        return "$( [Math]::Round($bytes / 1e9, 1) ) GB"
    }

    # The size of the whole image store in bytes, or -1 when it cannot be determined.
    #
    # The Images row of `docker system df` reports what the image store occupies on disk, counting a layer
    # shared by several images only once. Adding up the sizes from `docker image ls` instead would count each
    # shared layer once per image, and would report roughly three times the truth for a three-deep chain.
    #
    # Deliberately not `docker system df -v`, which recomputes the shared and unique size of every image and
    # takes minutes on an agent that holds hundreds of images. Deliberately not `--format json`, which only
    # recent versions of the Docker command line accept; the per-row template below is what the default table
    # is built from and has worked ever since `system df` was introduced.
    function Get-ImageStoreSize
    {
        $rows = @(docker system df --format '{{.Type}}|{{.Size}}' 2>&1)

        if ($LASTEXITCODE -ne 0)
        {
            Write-Host "Could not measure the Docker image store: $( ($rows -join ' ').Trim() )" -ForegroundColor Yellow
            return [long]-1
        }

        foreach ($row in $rows)
        {
            $fields = "$row" -split '\|', 2
            if ($fields.Count -ne 2 -or $fields[0].Trim() -ne 'Images')
            {
                continue
            }

            $size = ConvertFrom-DockerSize $fields[1]
            if ($size -lt 0)
            {
                Write-Host "Could not parse the image store size '$( $fields[1].Trim() )' reported by 'docker system df'." -ForegroundColor Yellow
            }

            return $size
        }

        Write-Host "'docker system df' reported no Images row, so the image store cannot be measured." -ForegroundColor Yellow
        return [long]-1
    }

    # Serializes the cleanup across the concurrent DockerBuild runs of one agent. Without it, two runs that both
    # measure the store 50 GB over budget each remove 50 GB, and the agent loses twice what it had to.
    #
    # This is an optimization, not the guarantee of correctness: runs under different accounts resolve different
    # temporary directories and never see each other's lock file. What actually keeps a sibling run's images
    # alive is the grace window. The operating system releases the handle when the process ends, so the lock
    # cannot go stale. Returns $null when another run holds it; the caller then skips the cleanup rather than
    # waiting, because whatever the other run frees, it frees for both.
    function Enter-ImageCleanupLock
    {
        try
        {
            # Resolved inside the try as well: a temporary directory that the platform rejects makes this throw
            # rather than the Open below.
            $lockPath = Join-Path ([System.IO.Path]::GetTempPath()) 'PostSharp.DockerBuild.ImageCleanup.lock'

            return [System.IO.File]::Open($lockPath, [System.IO.FileMode]::OpenOrCreate, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)
        }
        catch [System.IO.IOException]
        {
            # The expected case: another run holds the lock. Sharing violations, and every other input/output
            # error, arrive here.
            return $null
        }
        catch
        {
            # Anything else, such as a temporary directory that this account cannot write to, or a path the
            # platform rejects. UnauthorizedAccessException does not derive from IOException, so it would
            # otherwise escape, and $ErrorActionPreference is 'Stop' - which would fail the build over a
            # cleanup that is only ever best effort.
            Write-Host "Skipping the image-space cleanup: the lock file could not be opened. $( $_.Exception.Message )" -ForegroundColor Yellow
            return $null
        }
    }

    # Every image reference this run is about to need, so that the cleanup does not remove what the build
    # immediately rebuilds or pulls again.
    #
    # Docker already refuses to remove the ancestor of an image it keeps, so listing the whole chain only makes
    # the log readable and saves failed removal attempts. The OS image is the entry that genuinely matters: on a
    # run whose chain root is absent, no local image depends on the OS image yet, and on a Windows agent that
    # image is both the largest and the oldest one on the machine. Without this it would be the first candidate
    # removed, seconds before the root build asks for it.
    function Get-ImageChainKeepSet
    {
        $keep = [System.Collections.Generic.List[string]]::new()

        if ($RegistryImage)
        {
            # -RegistryImage skips all Dockerfile logic, so there is no chain to resolve and the single image
            # this run uses is the whole keep set.
            $keep.Add($RegistryImage)
            return $keep
        }

        # Resolve-ImageTag is pure and memoized, and Get-BaseDockerfile only reads the file, so the complete set
        # of tags this run needs is computed without a single call to the Docker engine.
        $current = $dockerfileFullPath
        $root = $current
        while ($current)
        {
            $keep.Add((Resolve-ImageTag $current))
            $root = $current
            $current = Get-BaseDockerfile $current
        }

        # $root is now the chain root, the only Dockerfile that declares an OS image.
        $osImageSpec = Get-OsImageSpec $root
        if ($osImageSpec)
        {
            $keep.Add("$( $osImageSpec.Repository ):$( $osImageSpec.Tag )")
            if ($dockerRegistry)
            {
                $keep.Add("$( Get-OsMirrorRepository $osImageSpec.Repository ):$( $osImageSpec.Tag )")
            }
        }

        return $keep
    }

    # The images that may be removed, oldest first.
    #
    # Whatever Docker itself refuses to delete is left to Docker: `docker image rm` refuses to remove an image
    # that a container references, or that a descendant is built on, and this function does not try to reproduce
    # that reasoning. It only produces a sensible order and drops what is pointless to attempt.
    #
    # `docker image ls` without -a is deliberate: -a also lists the intermediate images of the classic builder,
    # which are the whole build cache of the Windows engine.
    function Get-EvictionCandidates([System.Collections.Generic.HashSet[string]]$keepReferences, [datetime]$graceCutoff)
    {
        $rows = @(docker image ls --no-trunc --format '{{.ID}}|{{.Repository}}|{{.Tag}}|{{.CreatedAt}}|{{.Size}}' 2>&1)
        if ($LASTEXITCODE -ne 0)
        {
            Write-Host "Could not list the Docker images: $( ($rows -join ' ').Trim() )" -ForegroundColor Yellow
            return @()
        }

        # The image identifier of every container on the engine, running or stopped, which includes the boot
        # images of the other DockerBuild runs that share this agent. `docker ps -a --format '{{.Image}}'` would
        # give the reference as it was typed when the container was created, which is often a tag that has since
        # moved; inspecting the containers gives the identifier the container is really pinned to, which is also
        # what `docker image ls` reports.
        $inUse = [System.Collections.Generic.HashSet[string]]::new( [StringComparer]::OrdinalIgnoreCase )
        $containerIds = @(docker ps -a -q 2>$null | Where-Object { $_ -and $_.Trim() -ne '' })
        if ($containerIds.Count -gt 0)
        {
            foreach ($imageId in @(docker inspect --format '{{.Image}}' @containerIds 2>$null))
            {
                [void]$inUse.Add(("$imageId" -replace '^sha256:', ''))
            }
        }

        # One row per TAG, so two rows can carry the same image identifier (the same image tagged both locally
        # and with the registry prefix). They are grouped by identifier because the size must be counted once,
        # and because removing only some of an image's tags frees nothing at all: only the last tag deletes it.
        $byId = [ordered]@{ }
        $dateWarningIssued = $false

        foreach ($row in $rows)
        {
            $fields = "$row" -split '\|'
            if ($fields.Count -lt 5) { continue }

            $id = $fields[0] -replace '^sha256:', ''
            $repository = $fields[1]
            $tag = $fields[2]

            # Dangling images are handled up front by `docker image prune`, which applies Docker's own
            # definition of dangling. A '<none>' repository here is therefore already gone, or is the parent of
            # something, and is not a candidate either way.
            if ($repository -eq '<none>') { continue }

            if ($inUse.Contains($id)) { continue }

            $reference = "${repository}:${tag}"

            if (-not $byId.Contains($id))
            {
                # Docker prints CreatedAt as "2026-05-13 09:21:33 +0200 CEST", a Go layout that .NET cannot
                # parse as a whole. Every row is formatted in the same time zone, so the leading
                # "yyyy-MM-dd HH:mm:ss" alone orders them correctly and is all that is read. A date that cannot
                # be read is treated as brand new, and therefore never removed, rather than as ancient: if the
                # format ever changes, the result must be "frees nothing", never "deletes the oldest image on
                # the agent".
                $created = [datetime]::MaxValue
                if ($fields[3] -match '^(\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2})')
                {
                    [void][datetime]::TryParseExact($Matches[1], 'yyyy-MM-dd HH:mm:ss', [cultureinfo]::InvariantCulture, [Globalization.DateTimeStyles]::None, [ref]$created)
                }
                elseif (-not $dateWarningIssued)
                {
                    $dateWarningIssued = $true
                    Write-Host "Cannot read the creation date '$( $fields[3] )' that 'docker image ls' reports; an image whose date cannot be read is never removed." -ForegroundColor Yellow
                }

                $size = ConvertFrom-DockerSize $fields[4]

                $byId[$id] = [pscustomobject]@{
                    Id = $id
                    References = [System.Collections.Generic.List[string]]::new()
                    Created = $created
                    Size = $( if ($size -lt 0) { [long]0 } else { $size } )   # an unreadable size contributes nothing to the target
                    Keep = $false
                    Removed = $false
                }
            }

            # A tag in the keep set protects the whole image, not only that one tag: removing its other tags
            # would free nothing and would leave a half-untagged image behind.
            if ($keepReferences.Contains($reference)) { $byId[$id].Keep = $true }

            if ($tag -ne '<none>') { $byId[$id].References.Add($reference) }
        }

        # Oldest first. The Docker API exposes no last-used time for an image, so age is the only signal
        # available; the images this run is about to need are protected by the keep set instead.
        return @($byId.Values |
                Where-Object { -not $_.Keep -and $_.Created -lt $graceCutoff } |
                Sort-Object Created)
    }

    # Frees image disk space when the image store is over budget. Best effort throughout: this frees disk, it
    # does not gate the build, so nothing here may change the exit code of the script.
    #
    # Runs BEFORE the image chain is built, so that the space it frees is space this build can use, and before
    # the registry login, because everything it does is local to the Docker engine and needs no credentials.
    function Invoke-ImageSpaceCleanup([long]$budgetBytes, [string[]]$keepReferences)
    {
        $total = Get-ImageStoreSize
        if ($total -lt 0)
        {
            Write-Host "Skipping the image-space cleanup: the image store could not be measured." -ForegroundColor Yellow
            return
        }

        if ($total -le $budgetBytes)
        {
            Write-Host "Docker image store: $( Format-Gigabytes $total ) of the $( Format-Gigabytes $budgetBytes ) budget." -ForegroundColor Cyan
            return
        }

        Write-Host "Docker image store is $( Format-Gigabytes $total ), over the $( Format-Gigabytes $budgetBytes ) budget; freeing space." -ForegroundColor Yellow

        $lock = Enter-ImageCleanupLock
        if (-not $lock)
        {
            Write-Host "Another DockerBuild run is already freeing image space; skipping the cleanup." -ForegroundColor Yellow
            return
        }

        try
        {
            $started = [System.Diagnostics.Stopwatch]::StartNew()
            $graceCutoff = (Get-Date).AddHours(-$ImageCleanupGraceHours)
            $initial = $total

            # Dangling images first, through Docker's own prune, which applies Docker's definition of dangling
            # (untagged AND not the parent of anything) and so leaves the intermediate images of the Windows
            # classic builder alone. The `until` filter applies the same grace window as the removal below, and
            # closes the moment between a sibling run committing its last layer and tagging it, during which its
            # image is briefly indistinguishable from an orphan.
            #
            # Done separately, and first, because a dangling image shares almost every layer with the image that
            # replaced it. Inside the loop below its reported size would satisfy the whole overage on paper and
            # free nothing at all, wasting a pass.
            $pruneOutput = (docker image prune --force --filter "until=$( $ImageCleanupGraceHours )h" 2>&1 | Out-String)
            if ($pruneOutput -match 'Total reclaimed space:\s*(\S+)')
            {
                $reclaimed = ConvertFrom-DockerSize $Matches[1]
                if ($reclaimed -gt 0)
                {
                    Write-Host "  reclaimed $( Format-Gigabytes $reclaimed ) from dangling images" -ForegroundColor Gray

                    # Measure again rather than subtract, so that the loop below never removes space that has
                    # already been freed.
                    $total = Get-ImageStoreSize
                    if ($total -lt 0) { return }
                }
            }

            # The ?? guards the HashSet constructor, which rejects a null collection: a caller that resolved no
            # keep set at all must lose the cleanup, not the build.
            $keep = [System.Collections.Generic.HashSet[string]]::new( [string[]]($keepReferences ?? @()), [StringComparer]::OrdinalIgnoreCase )
            $candidates = Get-EvictionCandidates $keep $graceCutoff

            for ($pass = 1; $pass -le $ImageCleanupMaxPasses -and $total -gt $budgetBytes; $pass++)
            {
                if ($started.Elapsed.TotalMinutes -ge $ImageCleanupTimeoutMinutes)
                {
                    Write-Host "Giving up on the image-space cleanup after $ImageCleanupTimeoutMinutes minutes." -ForegroundColor Yellow
                    break
                }

                # Accumulate against the overage that was just measured. The size reported for an image includes
                # the layers it shares with images that survive, so this sum overstates what removing the batch
                # frees: the batch is guaranteed to free at most the overage, never more. That is the right
                # direction to be wrong in, because it costs passes and not images, and it is why the true total
                # is measured again after every pass instead of being tracked by subtraction.
                $overage = $total - $budgetBytes
                $batch = [System.Collections.Generic.List[object]]::new()
                $accumulated = [long]0

                foreach ($candidate in $candidates)
                {
                    if ($candidate.Removed) { continue }
                    $batch.Add($candidate)
                    $accumulated += $candidate.Size
                    if ($accumulated -ge $overage) { break }
                }

                if ($batch.Count -eq 0)
                {
                    Write-Host "No image is left to remove; the image store stays at $( Format-Gigabytes $total )." -ForegroundColor Yellow
                    break
                }

                Write-Host "Pass $pass`: removing up to $( $batch.Count ) unused image(s) to reclaim $( Format-Gigabytes $overage )." -ForegroundColor Cyan

                # Issue the removals NEWEST first, although the order of selection is oldest first: an image
                # cannot be removed while a descendant is built on it, and within a chain the descendant is the
                # younger image. Issuing them oldest first would fail on every parent. Repeat while anything is
                # still coming off, so that a parent freed by the removal of its child also goes in this pass.
                $removedInPass = 0
                for ($attempt = 0; $attempt -lt 4; $attempt++)
                {
                    $removedInAttempt = 0

                    foreach ($candidate in ($batch | Sort-Object Created -Descending))
                    {
                        if ($candidate.Removed) { continue }

                        # An image with no usable name and tag (a pull pinned to a digest) can only be addressed
                        # by its identifier. Docker refuses that when several repositories reference the image,
                        # which is handled below like any other refusal.
                        $references = if ($candidate.References.Count -gt 0) { $candidate.References } else { @($candidate.Id) }

                        $failure = $null
                        foreach ($reference in $references)
                        {
                            $output = (docker image rm $reference 2>&1 | Out-String).Trim()

                            # Deliberately not forced. `docker image rm -f` untags an image that a stopped
                            # container still holds, which is exactly how a concurrent run gets broken: the
                            # refusal below is the safety net that this whole function relies on.
                            #
                            # "No such image" means that another pass, or another run, got there first, which
                            # counts as success.
                            if ($LASTEXITCODE -ne 0 -and $output -notmatch 'No such image')
                            {
                                $failure = $output
                            }
                        }

                        if ($failure)
                        {
                            # The two expected refusals are that a container holds the image, possibly a sibling
                            # run's, and that a descendant protected by the keep set or the grace window is
                            # built on it. Both mean the image was correctly skipped, and are reported as detail
                            # rather than as a problem. An image that carries several tags loses the tags that
                            # were removed before the refusal; that frees nothing, but it also breaks nothing,
                            # because a container and a child image are both pinned to the identifier.
                            if ($failure -notmatch 'being used by|dependent child images|No such image')
                            {
                                Write-Host "  could not remove $( $references[0] ): $failure" -ForegroundColor Yellow
                            }
                        }
                        else
                        {
                            $candidate.Removed = $true
                            $removedInAttempt++
                            Write-Host "  removed $( $references -join ', ' )" -ForegroundColor Gray
                        }
                    }

                    $removedInPass += $removedInAttempt
                    if ($removedInAttempt -eq 0) { break }
                }

                if ($removedInPass -eq 0)
                {
                    # Every image in the batch was refused, and the list of candidates only ever shrinks, so
                    # another pass would select the same images and be refused again.
                    Write-Host "Nothing could be removed; the image store stays at $( Format-Gigabytes $total )." -ForegroundColor Yellow
                    break
                }

                $total = Get-ImageStoreSize
                if ($total -lt 0)
                {
                    # The store can no longer be measured, so there is no way to tell when to stop. Stopping is
                    # the only safe answer.
                    break
                }
            }

            $freed = $initial - $total
            if ($total -le $budgetBytes)
            {
                Write-Host "Freed $( Format-Gigabytes $freed ); the image store is now $( Format-Gigabytes $total ) of the $( Format-Gigabytes $budgetBytes ) budget." -ForegroundColor Green
            }
            else
            {
                Write-Host "Freed $( Format-Gigabytes $freed ), but the image store is still $( Format-Gigabytes $total ), over the $( Format-Gigabytes $budgetBytes ) budget." -ForegroundColor Yellow
            }
        }
        finally
        {
            $lock.Dispose()
        }
    }

    # Dictionary to track volume mounts with "writable wins" logic
    $script:VolumeMountDict = @{ }

    # Async registry push: each image is pushed in its own background job, started by Ensure-Image as soon as
    # the image exists, and waited for at the very end of the script. A push therefore overlaps the builds that
    # follow it and the container run.
    $script:RegistryPushJobs = @()

    # Set by Wait-ForRegistryPushes when any push failed, and read on both exit paths (normal completion and
    # the `finally` block).
    $script:PushFailed = $false

    # The exit code the script has decided on, so that the `finally` block can tell a build that is failing for
    # another reason from one that would otherwise have succeeded.
    $script:ExitCode = 0

    # OS images the root images are built FROM, keyed by upstream reference and resolved on first use by
    # Get-OsImage.
    $script:OsImages = @{ }

    # The Docker engine architecture, resolved on first use by Get-DockerArchitecture.
    $script:DockerArchitecture = $null

    # Tags already handed to Start-AsyncPush, so that resolving the same image twice (the run step resolves the
    # chain again) does not push it twice.
    $script:PushedTags = [System.Collections.Generic.HashSet[string]]::new( [StringComparer]::Ordinal )

    # Tag of the local, run-specific boot image (set by New-BootImage); removed after the container exits.
    $script:BootImageTag = $null

    function Add-VolumeMount
    {
        param(
            [Parameter(Mandatory = $true)]
            [string]$Path,
            [switch]$Writable
        )

        $normalizedPath = $Path.TrimEnd('\', '/')
        $normalizedKey = $normalizedPath.ToLower()
        $isGitDirectory = Test-Path (Join-Path $normalizedPath ".git")

        if ( $script:VolumeMountDict.ContainsKey($normalizedKey))
        {
            if ($Writable)
            {
                $script:VolumeMountDict[$normalizedKey].Writable = $true
            }
        }
        else
        {
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

    if ($RegistryImage)
    {
        # Use the pre-built registry image directly, skip all Dockerfile logic
        $ImageTag = $RegistryImage
        $NoBuildImage = $true
        Write-Host "Using registry image: $ImageTag" -ForegroundColor Cyan
    }
    else
    {
        # Single source of truth for the cache-busting stamp, shared by
        # Get-ContentHash (image tag, Claude mode only) and Get-TimestampFile
        # (update.timestamp file baked into the image). Computing it once here
        # guarantees both consumers see the same value even if the wall clock
        # crosses a week boundary mid-run.
        #
        # The stamp rotates once per UTC week (anchored to the Monday of the
        # current week) so the @latest npm installs of the Claude CLI and
        # marketplace plug-ins refresh weekly rather than daily. Use -Update to
        # force an immediate refresh regardless of the week boundary.
        $script:DayStamp = if ($Update)
        {
            [DateTime]::UtcNow.ToString("o")          # full ISO 8601, seconds precision
        }
        else
        {
            # Monday of the current UTC week (DayOfWeek: Sunday=0 .. Saturday=6).
            $utcToday = [DateTime]::UtcNow.Date
            $daysSinceMonday = ([int]$utcToday.DayOfWeek + 6) % 7
            $utcToday.AddDays(-$daysSinceMonday).ToString("yyyy-MM-dd")
        }

        # Determine which Dockerfile will be used.
        $DockerfilesDir = "$EngPath/docker"

        # Detect the Windows base-image tag. The OS variant is delivered as the WINDOWS_VERSION build-arg (and
        # folded into the content hash) rather than as a separate Dockerfile, so one chain serves both editions.
        # Windows build < 26100 is Windows Server 2022; otherwise Windows Server 2025.
        $windowsVersion = $null
        if ($IsWindows)
        {
            $osBuild = [System.Environment]::OSVersion.Version.Build
            $windowsVersion = if ($osBuild -lt 26100) { 'ltsc2022' } else { 'ltsc2025' }
            Write-Host "Detected Windows build $osBuild; using base image tag '$windowsVersion'" -ForegroundColor Cyan
        }

        if (-not $Dockerfile)
        {
            # Dockerfile names are prefix-free ("<layer>.Dockerfile"). -Claude targets the claude leaf; otherwise
            # the build leaf. The chain resolver walks ARG BASE_IMAGE to build/pull the ancestors first.
            $layer = if ($Claude) { 'claude' } else { 'build' }
            $Dockerfile = "$DockerfilesDir/$layer.Dockerfile"
        }

        # Get the full path of the Dockerfile
        if ( [System.IO.Path]::IsPathRooted($Dockerfile))
        {
            $dockerfileFullPath = $Dockerfile
        }
        else
        {
            $dockerfileFullPath = Join-Path $PSScriptRoot $Dockerfile
        }

        # Resolve the Docker registry for build images (env-based). Registry mode is off (local image tags) if
        # not set. Set before Resolve-ImageTag, which uses it to form the tag.
        # -NoRegistry suppresses the environment, which is how a host that cannot reach or verify the registry
        # still builds: the cost is that it builds every layer itself instead of pulling the ones already made.
        if ($NoRegistry)
        {
            Write-Host "Registry disabled by -NoRegistry; building images locally." -ForegroundColor Yellow
            $dockerRegistry = $null
        }
        else
        {
            $dockerRegistry = $env:DOCKER_REGISTRY
        }

        # Compute the target image tag (and, transitively, its ancestors' tags via ARG BASE_IMAGE). The image
        # name is the Dockerfile stem; the tag is its content hash (folding the parent tag, OS and day-stamp).
        $ImageTag = Resolve-ImageTag $dockerfileFullPath
        Write-Host "Target image tag: $ImageTag" -ForegroundColor Cyan
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
    # Collect environment variables for container (will be inlined in Init.g.ps1)
    if (-not $KeepInit)
    {
        # Create timestamp file for cache invalidation (only if building image). Build-OneImage stages it into
        # the context of each image that bakes it; when the run step rebuilds a Claude leaf without having come
        # through here, Copy-TimestampToContext creates it on demand.
        if (-not $NoBuildImage)
        {
            $script:TimestampFile = Get-TimestampFile
        }

        if ($Claude)
        {
            # Use Claude-specific environment variables (filtered and renamed)
            New-ClaudeEnvHashtable
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

            New-EnvHashtable -EnvironmentVariableList $EnvironmentVariables
        }

        # Allow the product repo to customize the container environment variables.
        # The optional script mutates the hashtable in place (add / change / remove keys)
        # and receives the leaf Dockerfile name and the mode as context.
        $customizeEnvScript = Join-Path $EngPath 'CustomizeDockerEnvironment.ps1'
        if (Test-Path $customizeEnvScript)
        {
            $dockerfileName = if ($Dockerfile) { Split-Path -Leaf $Dockerfile } else { '' }
            Write-Host "Customizing environment variables from $customizeEnvScript" -ForegroundColor Cyan
            . $customizeEnvScript `
                -ContainerEnvironmentVariables $script:ContainerEnvironmentVariables `
                -DockerfileName $dockerfileName `
                -Claude:([bool]$Claude)

            $sortedKeys = $script:ContainerEnvironmentVariables.Keys | Sort-Object
            Write-Host "Environment variables after customization: $( $sortedKeys -join ', ' )" -ForegroundColor Gray
        }
    }

    # Get the source directory name from $PSScriptRoot (script location)
    $SourceDirName = $PSScriptRoot

    # Start timing the entire process except cleaning
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

    # Ensure docker context directory exists (not needed for registry images)
    if (-not $RegistryImage -and -not (Test-Path $dockerContextDirectory))
    {
        New-Item -ItemType Directory -Path $dockerContextDirectory -Force | Out-Null
    }


    # Container user profile (matches actual user in container)
    $containerUserProfile = if ($IsUnix)
    {
        "/root"
    }
    else
    {
        "C:\Users\ContainerAdministrator"
    }

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
            Write-Host "Mounting source-dependencies directory: $( $sourceDirectory.FullName )" -ForegroundColor Cyan
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
                            $rwStatus = if ($isWritable)
                            {
                                "writable"
                            }
                            else
                            {
                                "readonly"
                            }
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
                    $rwStatus = if ($isWritable)
                    {
                        "writable"
                    }
                    else
                    {
                        "readonly"
                    }
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
    foreach ($key in $script:VolumeMountDict.Keys | Sort-Object)
    {
        $entry = $script:VolumeMountDict[$key]
        $mountOption = if ($entry.Writable)
        {
            ""
        }
        else
        {
            ":ro"
        }
        $VolumeMappings += "$( $entry.HostPath ):$( $entry.HostPath )$mountOption"
        $MountPoints += $entry.HostPath
        if ($entry.IsGitDirectory)
        {
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
                function ConvertTo-WslPath
                {
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
                    if ($parts[$i].Length -eq 1 -and $i + 1 -lt $parts.Length -and $parts[$i + 1] -match '^[\\/]')
                    {
                        # Windows path: C:\path - convert to WSL format
                        $hostPath = "$( $parts[$i] ):$( $parts[$i + 1] )"
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
                        if ($parts[$i].Length -eq 1 -and $i + 1 -lt $parts.Length -and $parts[$i + 1] -match '^[\\/]')
                        {
                            # Windows path - convert to WSL format
                            $containerPath = "$( $parts[$i] ):$( $parts[$i + 1] )"
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
                        $options = ':' + ($parts[$i..($parts.Length - 1)] -join ':')
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
        if ($PostInit)
        {
            $ContainerPostInit = Get-ContainerPath $PostInit
        }

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
        if ($PostInit)
        {
            $ContainerPostInit = $PostInit
        }

        # Deduplicate (case-sensitive for Unix paths)
        $VolumeMappings = $VolumeMappings | Sort-Object -Unique
        $MountPoints = $MountPoints | Sort-Object -Unique
        $GitDirectories = $GitDirectories | Sort-Object -Unique
    }

    # Create Init.g.ps1 with environment variables, git configuration (safe.directory and user identity)
    # This file is generated in $EngPath/.g/ (outside docker-context) and accessed via mounted directory
    if (-not $NoInit -and -not $KeepInit)
    {
        $gDirectory = Join-Path $EngPath ".g"
        if (-not (Test-Path $gDirectory))
        {
            New-Item -ItemType Directory -Path $gDirectory -Force | Out-Null
        }
        $initScript = Join-Path $gDirectory "Init.g.ps1"

        # Generate inline environment variable assignments
        $envVarAssignments = ""
        if ($script:ContainerEnvironmentVariables -and $script:ContainerEnvironmentVariables.Count -gt 0)
        {
            $envVarAssignments = "# Set environment variables`n"
            foreach ($key in $script:ContainerEnvironmentVariables.Keys | Sort-Object)
            {
                $value = $script:ContainerEnvironmentVariables[$key]
                # Escape single quotes in the value
                $escapedValue = $value -replace "'", "''"
                $envVarAssignments += "Write-Host `"Setting environment variable: $key`" -ForegroundColor Green`n"
                $envVarAssignments += "[Environment]::SetEnvironmentVariable('$key', '$escapedValue', [EnvironmentVariableTarget]::Machine)`n"
                $envVarAssignments += "`$env:$key='$escapedValue'`n"
            }
            $envVarAssignments += "`n"
        }

        # Generate git config commands directly from known values
        $gitConfigCommands = "# Configure git identity and safe.directory`n"

        if ($script:ContainerEnvironmentVariables -and $script:ContainerEnvironmentVariables.ContainsKey('GIT_USER_NAME'))
        {
            $escapedName = $script:ContainerEnvironmentVariables['GIT_USER_NAME'] -replace "'", "''"
            $gitConfigCommands += "git config --global user.name '$escapedName'`n"
        }
        if ($script:ContainerEnvironmentVariables -and $script:ContainerEnvironmentVariables.ContainsKey('GIT_USER_EMAIL'))
        {
            $escapedEmail = $script:ContainerEnvironmentVariables['GIT_USER_EMAIL'] -replace "'", "''"
            $gitConfigCommands += "git config --global user.email '$escapedEmail'`n"
        }

        # Generate git safe.directory commands directly
        foreach ($dir in $GitDirectories)
        {
            if ($dir)
            {
                # Git compares safe.directory against the repository path exactly, and the path it reports has
                # no trailing separator: registering only the trailing-slash form leaves the exception unmatched
                # and the repository still refused as dubiously owned. Register both forms.
                $normalizedDir = ($dir -replace '\\', '/').TrimEnd('/')
                $gitConfigCommands += "git config --global --add safe.directory '$normalizedDir'`n"
                $gitConfigCommands += "git config --global --add safe.directory '$normalizedDir/'`n"
            }
        }

        # Generate PostInit script call if specified
        $postInitCommands = ""
        if ($PostInit -and $ContainerPostInit)
        {
            $escapedPostInit = $ContainerPostInit -replace "'", "''"
            $postInitCommands = "`n# Execute PostInit script`n"
            $postInitCommands += "Write-Host `"Executing PostInit script: $ContainerPostInit`" -ForegroundColor Cyan`n"
            $postInitCommands += "& '$escapedPostInit'`n"
            $postInitCommands += "`$postInitExitCode = `$LASTEXITCODE`n"
            $postInitCommands += "if (`$postInitExitCode -and `$postInitExitCode -ne 0) { Write-Host `"PostInit script failed with exit code `$postInitExitCode.`" -ForegroundColor Red; exit `$postInitExitCode }`n"
        }

        $initScriptContent = @"
# Auto-generated initialization script for container startup

$envVarAssignments$gitConfigCommands$postInitCommands
"@

        # Write a test file with GUID first to check git tracking
        @"
# Test file - checking git tracking
# GUID: $([System.Guid]::NewGuid().ToString() )
"@ | Set-Content -Path $initScript -Encoding UTF8

        # Check if Init.g.ps1 is tracked by git
        $gitStatus = git status --porcelain $initScript 2> $null
        if ($LASTEXITCODE -eq 0 -and -not [string]::IsNullOrWhiteSpace($gitStatus))
        {
            Write-Error "Init script '$initScript' is tracked by git. Please add '$gDirectory' to .gitignore first."
            exit 1
        }

        $initScriptContent | Set-Content -Path $initScript -Encoding UTF8
    }

    # The cache-buster is staged by Build-OneImage, into the context of each image that actually declares the
    # `COPY .g/update.timestamp`. Doing it there rather than here is what makes it prefix-agnostic: the
    # destination is derived from the Dockerfile being built instead of from a hardcoded "claude" directory.

    # Path separator depends on platform (and container OS)
    $pathSeparator = if ($IsUnix)
    {
        ":"
    }
    else
    {
        ";"
    }
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

    # Free image disk space before the chain is resolved, so that what is freed is available to the builds and
    # pulls below rather than only to the next run. Everything the cleanup does is local to the Docker engine,
    # so it runs before the registry login and needs no credentials.
    #
    # Skipped when an existing container is reused: nothing is built or pulled then, so there is no space to
    # make room for, and the image of that container must stay untouched.
    if ($maxImageSpaceBytes -gt 0 -and -not $existingContainerId)
    {
        Invoke-ImageSpaceCleanup $maxImageSpaceBytes (Get-ImageChainKeepSet)
    }

    # Registry authentication + image-chain resolution (build the ancestor chain; pull-or-build+push each level).
    $builtNewImage = $false
    $dockerConfigArg = @()

    # $RegistryImage is an explicit user override ("use this pre-built image, skip all Dockerfile logic"), so the
    # chain is neither authenticated nor resolved for it. Every other path (build step, default run, and the CI run
    # step with -NoBuildImage) resolves the chain so the local-only Claude leaf is guaranteed present below.
    if (-not $existingContainerId -and -not $RegistryImage)
    {
        if ($dockerRegistry)
        {
            # Temporary Docker config dir to avoid credential-helper issues (e.g. docker-credential-desktop not
            # found when using Docker Engine without Desktop), then authenticate.
            #
            # Authentication runs for BOTH the build step (-BuildImage) and the run step (-NoBuildImage). The run
            # step still resolves the chain below, and when it executes on a different docker daemon than the build
            # step it must PULL the stable ancestors (vs/build) from the registry - so credentials must be present
            # here too. (The Claude leaf is never pulled; it is always built locally - see Ensure-Image.)
            $tempDockerConfig = Join-Path ([System.IO.Path]::GetTempPath()) "docker-config-$( New-Guid )"
            New-Item -ItemType Directory -Path $tempDockerConfig -Force | Out-Null
            @{ auths = @{ } } | ConvertTo-Json | Set-Content (Join-Path $tempDockerConfig "config.json")
            $dockerConfigArg = @("--config", $tempDockerConfig)

            $dockerPassword = $env:DOCKER_PASSWORD
            $dockerUsername = $env:DOCKER_USERNAME

            # Setting DOCKER_REGISTRY without credentials is a configuration error, not a request for anonymous
            # access: continuing would silently rebuild every ancestor locally (no pull) and then fail the pushes
            # at the end of the build anyway, after a long and misleading build.
            $missingCredentials = @()
            if (-not $dockerUsername) { $missingCredentials += "DOCKER_USERNAME" }
            if (-not $dockerPassword) { $missingCredentials += "DOCKER_PASSWORD" }
            if ($missingCredentials)
            {
                Write-Error "DOCKER_REGISTRY is set to '$dockerRegistry' but $( $missingCredentials -join " and " ) $( if ($missingCredentials.Count -eq 1) { "is" } else { "are" } ) not set. Set the missing variable(s), or unset DOCKER_REGISTRY to build without a registry."
                exit 1
            }

            Write-Host "Authenticating to registry..." -ForegroundColor Gray
            $loginOutput = $dockerPassword | docker @dockerConfigArg login $dockerRegistry --username $dockerUsername --password-stdin 2>&1
            if ($LASTEXITCODE -ne 0)
            {
                Write-Error "Registry authentication to '$dockerRegistry' failed as user '$dockerUsername': $( $loginOutput -join [System.Environment]::NewLine )"
                exit 1
            }
        }

        # Resolve the whole chain (parent first): use local, else pull ancestors, else build; freshly built layers
        # are queued for push. This runs in the run step (-NoBuildImage) too: Ensure-Image is idempotent (it is a
        # no-op for images already present locally), so when the build step shared this daemon nothing is rebuilt.
        # Its purpose here is to guarantee the local-only Claude leaf exists before the boot image's FROM resolves
        # it - the leaf is never pushed, so it cannot be pulled and MUST be (re)built locally in the run step.
        Ensure-Image $dockerfileFullPath | Out-Null
    }
    elseif ($existingContainerId)
    {
        Write-Host "Skipping image build (reusing existing container $existingContainerId)." -ForegroundColor Yellow
    }
    else
    {
        Write-Host "Skipping image build (using pre-built registry image $ImageTag)." -ForegroundColor Yellow
    }

    # Build the local boot image over the resolved chain image (creates the bind-mount directories). The static
    # chain images stay pure and shareable; this thin layer carries the machine-specific mount set and is never
    # pushed. `docker run` below uses the boot image. Its `FROM` resolves the chain leaf locally: Ensure-Image
    # above guarantees the leaf is present (the Claude leaf is built locally, ancestors are local-or-pulled), so
    # the boot build never pulls and needs no registry credentials (same as the chain builds in Build-OneImage).
    if (-not $BuildImage -and -not $existingContainerId -and $mountPointsAsString)
    {
        $ImageTag = New-BootImage $ImageTag
    }

    # Run the build within the container
    if (-not $BuildImage)
    {
        # Common setup for both Claude and normal build modes
        $pwshPath = if ($IsUnix)
        {
            '/usr/bin/pwsh'
        }
        else
        {
            'C:\Program Files\PowerShell\7\pwsh.exe'
        }
        # Init.g.ps1 is in the mounted source directory, not baked into the image
        # Init.g.ps1 is in $EngPath/.g/ (outside docker-context), accessed via mounted source directory
        $containerInitScript = "$ContainerSourceDir/$EngPath/.g/Init.g.ps1"
        $initCall = if (-not $NoInit)
        {
            "& '$containerInitScript'; "
        }
        else
        {
            ""
        }

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
            $hostUserProfile = if ($IsUnix)
            {
                $env:HOME
            }
            else
            {
                $env:USERPROFILE
            }

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
            if ( [System.IO.Path]::IsPathRooted($Script))
            {
                $scriptFullPath = $Script
            }
            else
            {
                $scriptFullPath = Join-Path $ContainerSourceDir $Script
            }
            # Fail if the script is not there. In -Command mode `& <missing>` is a non-terminating error and
            # leaves $LASTEXITCODE at 0, so without this guard the container exits 0 and the build is reported
            # successful having run nothing at all. That is what an unmounted workspace looks like: the bind
            # mount silently yields an empty directory when the engine is not allowed to share the host path.
            $missingScriptMessage = "The script $scriptFullPath is not present in the container. The workspace " +
                "is most likely not mounted -- check that the agent work directory is a path the container " +
                "engine is allowed to share."

            $scriptInvocation = "if ( -not ( Test-Path -LiteralPath '$scriptFullPath' ) ) " +
                "{ Write-Host '$missingScriptMessage' -ForegroundColor Red; exit 127 }; & '$scriptFullPath'"
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

            # Memory limit: everywhere except Windows process isolation, which ignores it.
            if ($supportsResourceLimits -and $Memory)
            {
                $dockerCmd += "--memory=$Memory"
            }

            # The MSBuild node count that matches that budget. msbuild.ps1 reads it inside the container, where the
            # limit itself is not visible.
            if ($maxBuildParallelism -gt 0)
            {
                $dockerCmd += @('-e', "MAX_BUILD_PARALLELISM=$maxBuildParallelism")
            }

            # CPU limit: dynamic or static
            if ($isDynamicCpus)
            {
                $dynamicAllocation = Invoke-DynamicCpuRebalance -AdditionalContainers 1
                $dockerCmd += "--cpus=$dynamicAllocation"
                $dockerCmd += @('-e', "DOTNET_PROCESSOR_COUNT=$dynamicAllocation")
                $dockerCmd += @('--label', "$DynamicCpuLabel")
            }
            elseif ($supportsResourceLimits)
            {
                $dockerCmd += "--cpus=$Cpus"
            }

            if ($isolationArg)
            {
                $dockerCmd += $isolationArg
            }
            $dockerCmd += $dockerArgs
            $dockerCmd += $volumeArgs
            $dockerCmd += $envArgs

            # Add port mappings from -Ports parameter
            if ($Ports -and $Ports.Count -gt 0)
            {
                foreach ($portMapping in $Ports)
                {
                    $dockerCmd += @('-p', $portMapping)
                }
            }

            # Add label for container identification (used for cleanup of orphaned containers)
            if ($Label)
            {
                $dockerCmd += @('--label', "postsharp.build=$Label")
            }

            if ($pwshArgs)
            {
                $dockerCmd += @('-w', $ContainerCallingDir, $ImageTag, $pwshPath, $pwshArgs, '-Command', $inlineScript)
            }
            else
            {
                $dockerCmd += @('-w', $ContainerCallingDir, $ImageTag, $pwshPath, '-Command', $inlineScript)
            }

            Write-Host "Executing: ``docker $( $dockerCmd -join ' ' )" -ForegroundColor Cyan
            & docker @dockerCmd
        }
        $dockerExitCode = $LASTEXITCODE

        # Post-exit rebalance: when our container exits (--rm removes it),
        # redistribute CPUs to remaining managed containers
        if ($isDynamicCpus -and -not $existingContainerId)
        {
            Invoke-DynamicCpuRebalance -AdditionalContainers 0 | Out-Null
        }

        # Check exit code
        if ($dockerExitCode -ne 0)
        {
            Write-Host "Container failed with exit code $dockerExitCode" -ForegroundColor Red
        }
    }
    else
    {
        Write-Host "Skipping container run (BuildImage specified)." -ForegroundColor Yellow
    }

    Wait-ForRegistryPushes

    # Stop timing and display results
    $elapsed = $stopwatch.Elapsed
    Write-Host ""
    Write-Host "Total build time: $($elapsed.ToString('hh\:mm\:ss\.fff') )" -ForegroundColor Cyan
    Write-Host "Build completed at: $( Get-Date -Format 'yyyy-MM-dd HH:mm:ss' )" -ForegroundColor Cyan

    # The container's own exit code wins when both failed: it says more about what went wrong than the push does.
    # $dockerExitCode is $null when the container was not run at all (-BuildImage).
    if ($dockerExitCode)
    {
        $script:ExitCode = $dockerExitCode
    }
    elseif ($script:PushFailed)
    {
        Write-Host "The build failed because at least one registry push failed." -ForegroundColor Red
        $script:ExitCode = 1
    }

    exit $script:ExitCode
}
finally
{
    # Never abandon a push that is still in flight. On the normal path the jobs have already been waited for
    # (and the list emptied), so this only does something when a later step failed or was interrupted - exactly
    # when the push would otherwise die with the process and leave the image out of the registry.
    if ($script:RegistryPushJobs.Count -gt 0)
    {
        Wait-ForRegistryPushes
    }

    # Safety-net rebalance on Ctrl+C or unexpected exit
    if ($isDynamicCpus)
    {
        try { Invoke-DynamicCpuRebalance -AdditionalContainers 0 | Out-Null } catch { }
    }

    # Remove the run-specific boot image. Its tag is unique per run (timestamp suffix), so leaving it behind
    # would accumulate dangling leaf images. Running here (not after the run) guarantees removal even when the
    # build throws or the user presses Ctrl+C - PowerShell still executes finally on a pipeline interrupt. The
    # `docker run --rm` removes the container, so the image is normally unreferenced; `-f` untags it even if a
    # stopped container still references it. Best-effort: a removal failure must not mask the build's exit code.
    if ($script:BootImageTag)
    {
        Write-Host "Removing boot image $($script:BootImageTag)" -ForegroundColor Gray
        docker image rm -f $script:BootImageTag *> $null
    }

    # Restore original location
    Pop-Location

    # A failed push must fail the build even when the script is unwinding from another failure. A non-zero
    # $script:ExitCode means the script already decided to fail, and that reason is the more informative one.
    if ($script:PushFailed -and $script:ExitCode -eq 0)
    {
        Write-Host "The build failed because at least one registry push failed." -ForegroundColor Red
        exit 1
    }
}
