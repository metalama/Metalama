// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using System;
using System.Linq;
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
        reporter.ReportException( new InvalidOperationException() );

        Assert.NotEmpty( this.FileSystem.EnumerateFiles( this._crashReportsDirectory, "*.txt" ) );
    }

    // The exception toast is no longer shown by LocalExceptionReporter (which now only writes the human-readable local
    // crash report). It is shown by ExceptionReporter once the scrubbed report is captured, so that clicking it can
    // open the worker review page for that exact report. See ReportExceptionTests.ToastOpensReviewPage and #1674.
    [Fact]
    public void CrashReportContainsReportingCallStack()
    {
        var reporter = new LocalExceptionReporter( this.ServiceProvider );
        reporter.ReportException( new InvalidOperationException( "Test exception" ) );

        var files = this.FileSystem.EnumerateFiles( this._crashReportsDirectory, "*.txt" ).ToList();
        Assert.Single( files );

        var content = this.FileSystem.ReadAllText( files[0] );
        Assert.Contains( "===== Reporting Call Stack =====", content, StringComparison.Ordinal );
        Assert.Contains( "ReportException", content, StringComparison.Ordinal );
    }

    [Fact]
    public void CrashReportScrubsSecretsInExceptionMessage()
    {
        // The exception message (emitted both standalone and via Exception.ToString()) must be scrubbed. See #1680.
        var reporter = new LocalExceptionReporter( this.ServiceProvider );
        reporter.ReportException( new InvalidOperationException( "Login failed for password=SuperSecret123" ) );

        var content = this.FileSystem.ReadAllText( this.FileSystem.EnumerateFiles( this._crashReportsDirectory, "*.txt" ).Single() );
        this.Logger.WriteLine( content );

        Assert.DoesNotContain( "SuperSecret123", content, StringComparison.Ordinal );
        Assert.Contains( "#secret", content, StringComparison.Ordinal );
    }

    [Fact]
    public void CrashReportScrubsCommandLine()
    {
        // The local crash report records Environment.CommandLine, which contains absolute host paths (and
        // potentially the OS user name). It must be scrubbed rather than written verbatim. See #1680.
        var reporter = new LocalExceptionReporter( this.ServiceProvider );
        reporter.ReportException( new InvalidOperationException( "test" ) );

        var content = this.FileSystem.ReadAllText( this.FileSystem.EnumerateFiles( this._crashReportsDirectory, "*.txt" ).Single() );
        this.Logger.WriteLine( content );

        Assert.DoesNotContain( Environment.CommandLine, content, StringComparison.Ordinal );
    }
}