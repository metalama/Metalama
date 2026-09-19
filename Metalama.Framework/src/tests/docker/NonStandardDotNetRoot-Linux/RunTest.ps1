param(
    [Parameter( Mandatory = $true )] [string] $Platform
)

$ErrorActionPreference = 'Stop'

$repositoryRoot = ( Resolve-Path ( Join-Path $PSScriptRoot '../../../../..' ) ).Path
$os = if ($Platform -like 'linux-*') { 'linux' } else { 'windows' }

# A hashtable splatted with @, never the literal passed positionally: an array or a bare hashtable lands in
# -BuildArgs, and DockerBuild.ps1 then runs an ordinary product build, with the product secrets, instead of the
# test container. The script refuses that, but the correct form is written here rather than relying on it.
$arguments = @{
    Test = $true
    OS = $os
    Dockerfile = ( Join-Path $PSScriptRoot 'Dockerfile' )

    # The command runs with the mounted repository as its working directory, so test.ps1 is addressed by its path
    # in the repository. It locates its own fixtures from $PSScriptRoot, as it always has.
    Command = 'pwsh -NoProfile -File Metalama.Framework/src/tests/docker/NonStandardDotNetRoot-Linux/test.ps1'
}

& ( Join-Path $repositoryRoot 'DockerBuild.ps1' ) @arguments

exit $LASTEXITCODE
