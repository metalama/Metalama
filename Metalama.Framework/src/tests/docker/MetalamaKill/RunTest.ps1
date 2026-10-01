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
    #
    # The command is compound, so that sh does not replace itself with pwsh and stays the first process of the container.
    # The compiler server outlives the build that started it and is adopted by the first process when its parent exits;
    # sh reaps it when it exits, whereas pwsh would leave it as a zombie, which the wait of 'metalama shutdown' would take
    # for a running process. A bare 'exit' returns the status of pwsh without a '$', which the hop of DockerBuild.ps1 into
    # WSL on a Windows development machine would expand (postsharp-ops/PostSharp.Engineering#167).
    Command = 'pwsh -NoProfile -File Metalama.Framework/src/tests/docker/MetalamaKill/test.ps1; exit'
}

& ( Join-Path $repositoryRoot 'DockerBuild.ps1' ) @arguments

exit $LASTEXITCODE
