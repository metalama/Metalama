@{
    # Two .NET installations of different architectures on one machine, which only Windows provides.
    #
    # The timeout is twice that of the Linux tests because a Windows Server Core image is several gigabytes,
    # and an agent meeting it for the first time spends that time pulling rather than testing.
    Platforms = @( 'win-x64' )

    TimeoutSeconds = 3600
}
