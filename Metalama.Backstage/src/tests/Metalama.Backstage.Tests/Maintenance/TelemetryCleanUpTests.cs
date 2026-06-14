// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Maintenance;
using Metalama.Backstage.Testing;
using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Maintenance;

public sealed class TelemetryCleanUpTests : TestsBase
{
    private readonly IStandardDirectories _standardDirectories;

    public TelemetryCleanUpTests( ITestOutputHelper logger ) : base( logger )
    {
        this._standardDirectories = this.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>();

        // Create the base directories used by the maintenance pass.
        this.FileSystem.CreateDirectory( this._standardDirectories.ApplicationDataDirectory );
        this.FileSystem.CreateDirectory( this._standardDirectories.TempDirectory );
    }

    protected override void ConfigureServices( ServiceProviderBuilder services )
    {
        services
            .AddSingleton<IApplicationInfoProvider>( new ApplicationInfoProvider( new TestApplicationInfo() ) );
    }

    private string GetTelemetryDirectory( string subdirectory )
        => Path.Combine( this._standardDirectories.ApplicationDataDirectory, "Telemetry", subdirectory );

    /// <summary>
    /// Regression test for #1679: the telemetry data tree under <c>ApplicationDataDirectory\Telemetry</c>
    /// must be cleaned up by the once-per-day maintenance pass. Files older than the retention period
    /// (default 30 days) are deleted; newer files are kept.
    /// </summary>
    [Fact]
    public void TelemetryFilesOlderThanRetentionAreDeleted()
    {
        var exceptionsDirectory = this.GetTelemetryDirectory( "Exceptions" );
        this.FileSystem.CreateDirectory( exceptionsDirectory );

        var oldFile = Path.Combine( exceptionsDirectory, "old-report.xml" );
        var newFile = Path.Combine( exceptionsDirectory, "new-report.xml" );

        // Create an old file, then advance time well past the default 30-day retention period.
        this.FileSystem.WriteAllText( oldFile, "old" );
        this.Time.AddTime( TimeSpan.FromDays( 40 ) );

        // Create a recent file.
        this.FileSystem.WriteAllText( newFile, "new" );
        this.Time.AddTime( TimeSpan.FromDays( 5 ) );

        // Run the once-per-day maintenance pass (forced to ignore the schedule).
        var tempFileManager = new TempFileManager( this.ServiceProvider );
        tempFileManager.CleanTempDirectories( true );

        // The old file (45 days) must be deleted; the recent file (5 days) must be kept.
        Assert.False( this.FileSystem.FileExists( oldFile ), "The telemetry file older than the retention period should have been deleted." );
        Assert.True( this.FileSystem.FileExists( newFile ), "The telemetry file newer than the retention period should have been kept." );
    }
}
