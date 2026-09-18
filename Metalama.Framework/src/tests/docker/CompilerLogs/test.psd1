@{
    # The image installs the .NET SDK from a tarball onto a plain Ubuntu, which is x64 only until that is
    # parameterized.
    Platforms = @( 'linux-x64' )

    TimeoutSeconds = 1800
}
