@{
    # The test reads the log directory from the same rule Backstage applies, so it is not tied to one
    # platform any more. It stays on Linux because that is where the compiler is exercised here.
    Platforms = @( 'linux-x64' )

    TimeoutSeconds = 1800
}
