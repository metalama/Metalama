@{
    # The Windows half of the test. See NonStandardDotNetRootLinux for why the two are separate.
    #
    # The timeout is twice that of the Linux tests because a Windows Server Core image is several gigabytes,
    # and an agent meeting it for the first time spends that time pulling rather than testing.
    Platforms = @( 'win-x64' )

    TimeoutSeconds = 3600
}
