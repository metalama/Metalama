// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Testing;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Telemetry;

public sealed class TelemetryDirectoryTests : TestsBase
{
    public TelemetryDirectoryTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public void TelemetryDirectoriesShouldBeUnderApplicationLocalDataDirectory()
    {
        var directories = this.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>();
        var localDataDirectory = directories.ApplicationLocalDataDirectory;

        Assert.StartsWith( localDataDirectory, directories.TelemetryUploadQueueDirectory, StringComparison.OrdinalIgnoreCase );
        Assert.StartsWith( localDataDirectory, directories.TelemetryLogsDirectory, StringComparison.OrdinalIgnoreCase );
        Assert.StartsWith( localDataDirectory, directories.TelemetryExceptionsDirectory, StringComparison.OrdinalIgnoreCase );
        Assert.StartsWith( localDataDirectory, directories.TelemetryUploadPackagesDirectory, StringComparison.OrdinalIgnoreCase );
    }

    [Fact]
    public void ApplicationLocalDataDirectoryShouldNotBeUnderTempDirectory()
    {
        var directories = this.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>();

        Assert.False(
            directories.ApplicationLocalDataDirectory.StartsWith( directories.TempDirectory, StringComparison.OrdinalIgnoreCase ),
            $"ApplicationLocalDataDirectory '{directories.ApplicationLocalDataDirectory}' should not be under TempDirectory '{directories.TempDirectory}'." );
    }
}
