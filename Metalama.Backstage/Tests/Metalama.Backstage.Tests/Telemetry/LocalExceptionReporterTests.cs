// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Telemetry;

public sealed class LocalExceptionReporterTests : TestsBase
{
    private readonly string _crashReportsDirectory;

    public LocalExceptionReporterTests( ITestOutputHelper logger ) : base( logger )
    {
        this._crashReportsDirectory = this.ServiceProvider.GetRequiredBackstageService<IStandardDirectories>().CrashReportsDirectory;
    }

    [Fact]
    public void CrashReportCreatedWhenNotProvided()
    {
        var reporter = new LocalExceptionReporter( this.ServiceProvider );
        reporter.ReportException( new InvalidOperationException(), null );

        Assert.NotEmpty( this.FileSystem.EnumerateFiles( this._crashReportsDirectory, "*.txt" ) );
    }

    [Fact]
    public void CrashReportNotCreatedWhenProvided()
    {
        var reporter = new LocalExceptionReporter( this.ServiceProvider );
        reporter.ReportException( new InvalidOperationException(), "currentReport.txt" );

        Assert.Empty( this.FileSystem.EnumerateFiles( this._crashReportsDirectory, "*.txt" ) );
    }

    [Fact]
    public void ToastNotificationReported()
    {
        var reporter = new LocalExceptionReporter( this.ServiceProvider );
        reporter.ReportException( new InvalidOperationException(), "currentReport.txt" );
        Assert.NotEmpty( this.UserInterface.Notifications );
    }
}