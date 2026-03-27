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
    public void TelemetryUploadQueueDirectoryShouldNotBeUnderApplicationData()
    {
        var directories = this.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>();
        var applicationDataDirectory = directories.ApplicationDataDirectory;
        var telemetryUploadQueueDirectory = directories.TelemetryUploadQueueDirectory;

        Assert.False(
            telemetryUploadQueueDirectory.StartsWith( applicationDataDirectory, StringComparison.OrdinalIgnoreCase ),
            $"TelemetryUploadQueueDirectory '{telemetryUploadQueueDirectory}' should not be under ApplicationDataDirectory '{applicationDataDirectory}'." );
    }

    [Fact]
    public void TelemetryLogsDirectoryShouldNotBeUnderApplicationData()
    {
        var directories = this.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>();
        var applicationDataDirectory = directories.ApplicationDataDirectory;
        var telemetryLogsDirectory = directories.TelemetryLogsDirectory;

        Assert.False(
            telemetryLogsDirectory.StartsWith( applicationDataDirectory, StringComparison.OrdinalIgnoreCase ),
            $"TelemetryLogsDirectory '{telemetryLogsDirectory}' should not be under ApplicationDataDirectory '{applicationDataDirectory}'." );
    }

    [Fact]
    public void TelemetryExceptionsDirectoryShouldNotBeUnderApplicationData()
    {
        var directories = this.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>();
        var applicationDataDirectory = directories.ApplicationDataDirectory;
        var telemetryExceptionsDirectory = directories.TelemetryExceptionsDirectory;

        Assert.False(
            telemetryExceptionsDirectory.StartsWith( applicationDataDirectory, StringComparison.OrdinalIgnoreCase ),
            $"TelemetryExceptionsDirectory '{telemetryExceptionsDirectory}' should not be under ApplicationDataDirectory '{applicationDataDirectory}'." );
    }

    [Fact]
    public void TelemetryUploadPackagesDirectoryShouldNotBeUnderApplicationData()
    {
        var directories = this.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>();
        var applicationDataDirectory = directories.ApplicationDataDirectory;
        var telemetryUploadPackagesDirectory = directories.TelemetryUploadPackagesDirectory;

        Assert.False(
            telemetryUploadPackagesDirectory.StartsWith( applicationDataDirectory, StringComparison.OrdinalIgnoreCase ),
            $"TelemetryUploadPackagesDirectory '{telemetryUploadPackagesDirectory}' should not be under ApplicationDataDirectory '{applicationDataDirectory}'." );
    }

    [Fact]
    public void TelemetryDirectoriesShouldBeUnderTempDirectory()
    {
        var directories = this.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>();
        var tempDirectory = directories.TempDirectory;

        Assert.StartsWith( tempDirectory, directories.TelemetryUploadQueueDirectory, StringComparison.OrdinalIgnoreCase );
        Assert.StartsWith( tempDirectory, directories.TelemetryLogsDirectory, StringComparison.OrdinalIgnoreCase );
        Assert.StartsWith( tempDirectory, directories.TelemetryExceptionsDirectory, StringComparison.OrdinalIgnoreCase );
        Assert.StartsWith( tempDirectory, directories.TelemetryUploadPackagesDirectory, StringComparison.OrdinalIgnoreCase );
    }
}
