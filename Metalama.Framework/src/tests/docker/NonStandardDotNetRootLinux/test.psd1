@{
    # The Linux half of the test. The Windows half is a separate directory rather than another platform here,
    # because the two need different images and a test directory holds one Dockerfile.
    Platforms = @( 'linux-x64' )

    TimeoutSeconds = 1800
}
