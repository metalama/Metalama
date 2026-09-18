<#
.SYNOPSIS
    Verifies that the compiler writes the logs that its diagnostics configuration asks for.

.DESCRIPTION
    This script runs on the host and does nothing but start the container. What the test does is in
    Container.ps1, which runs inside it, because it needs a .NET SDK and the product packages and the host is
    required to have neither.
#>
param(
    [Parameter( Mandatory = $true )] [string] $Platform
)

$ErrorActionPreference = 'Stop'

# The engine is chosen by the platform rather than by the host. On a Windows development machine the Linux engine
# is the one inside WSL, and DockerBuild.ps1 makes that hop itself when it is told which operating system is meant.
$os = if ($Platform -like 'linux-*') { 'linux' } else { 'windows' }

$repositoryRoot = Resolve-Path "$PSScriptRoot/../../../../.."

# The repository is mounted and the command runs with the repository as its working directory, so the script is
# named by its path relative to the root.
$arguments = @{
    Test = $true
    OS = $os
    Dockerfile = "$PSScriptRoot/Dockerfile"
    Command = 'pwsh -NoProfile -NonInteractive -File Metalama.Framework/src/tests/docker/CompilerLogs/Container.ps1'
}

& "$repositoryRoot/DockerBuild.ps1" @arguments

exit $LASTEXITCODE
