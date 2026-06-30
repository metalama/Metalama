// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Testing;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Infrastructure;

// Tests for the temp-directory location introduced by issue #1650. Metalama loads and executes assemblies from its
// temp directory, so on Unix it must live under the user-private application-data directory, not the world-writable
// /tmp. The OS is driven by the injected IRuntimeInformation (TestRuntimeInformation.Platform), so both the Windows and
// Unix branches are exercised regardless of the platform the test runs on.
public sealed class StandardDirectoriesTests : TestsBase
{
    public StandardDirectoriesTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public void Windows_TempDirectoryIsUnderUserTempPath_AndHasNoLegacyDirectories()
    {
        this.RuntimeInformation.Platform = OSPlatform.Windows;

        var directories = new StandardDirectories( this.ServiceProvider );

        // On Windows the user temp directory is already private to the current user.
        Assert.Equal( Path.Combine( Path.GetTempPath(), "Metalama" ), directories.TempDirectory );
        Assert.Empty( directories.LegacyTempDirectories );
    }

    [Fact]
    public void Unix_TempDirectoryIsUnderApplicationData_AndLegacyIsTmp()
    {
        this.RuntimeInformation.Platform = OSPlatform.Linux;

        var directories = new StandardDirectories( this.ServiceProvider );

        // Must be under the user-private application-data directory, and never under the world-writable /tmp (#1650).
        Assert.Equal( Path.Combine( directories.ApplicationDataDirectory, "Temp" ), directories.TempDirectory );
        Assert.False( directories.TempDirectory.StartsWith( Path.GetTempPath(), StringComparison.Ordinal ) );

        // The pre-#1650 location must be offered to the cache clean-up.
        var legacy = Assert.Single( directories.LegacyTempDirectories );
        Assert.Equal( Path.Combine( Path.GetTempPath(), "Metalama" ), legacy );
    }

    [Fact]
    public void Override_TempDirectoryIsUnderOverride_AndHasNoLegacyDirectories()
    {
        // METALAMA_TEMP takes precedence on any platform, and there is then no legacy location.
        this.RuntimeInformation.Platform = OSPlatform.Linux;
        var overridePath = Path.Combine( Path.GetTempPath(), "CustomMetalamaTemp" );
        this.EnvironmentVariableProvider.Environment["METALAMA_TEMP"] = overridePath;

        var directories = new StandardDirectories( this.ServiceProvider );

        Assert.Equal( Path.Combine( overridePath, "Metalama" ), directories.TempDirectory );
        Assert.Empty( directories.LegacyTempDirectories );
    }
}
