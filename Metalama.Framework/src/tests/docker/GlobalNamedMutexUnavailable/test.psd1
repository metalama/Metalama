@{
    # The condition under test is a Unix one: the runtime backs a machine-wide mutex with files under
    # /tmp/.dotnet, which is what this test makes unusable. See Container.ps1 for the whole reasoning.
    Platforms = @( 'linux-x64' )

    TimeoutSeconds = 1800
}
