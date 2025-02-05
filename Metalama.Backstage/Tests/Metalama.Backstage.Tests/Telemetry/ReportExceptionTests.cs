// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Telemetry;

public class ReportExceptionTests : TestsBase
{
    public ReportExceptionTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } ) { }

    protected override void OnAfterServicesCreated( Services services )
        => services.ConfigurationManager!.Update<TelemetryConfiguration>(
            c => c with { ExceptionReportingAction = ReportingAction.Yes, PerformanceProblemReportingAction = ReportingAction.Yes } );

    private static string CreateStackFrame( string methodName, int lineNumber )
        => $"   at Metalama.Backstage.Tests.Telemetry.ReportExceptionTests.{methodName}() in C:\\src\\Metalama.Backstage\\Tests\\Metalama.Backstage.Tests\\Telemetry\\ReportExceptionTests.cs:line {lineNumber}";

    private static string CreateStackTrace( IEnumerable<(string MethodName, int LineNumber)> methods )
        => string.Join( Environment.NewLine, methods.Select( m => CreateStackFrame( m.MethodName, m.LineNumber ) ) );

    private static AggregateException CreateAggregateException( string message, IEnumerable<Exception> innerExceptions )
    {
        try
        {
            throw new AggregateException( message, innerExceptions );
        }
        catch ( AggregateException e )
        {
            return e;
        }
    }

    private void AssertFilesCount( int expectedCount )
    {
        this.Logger.WriteLine( "Files:" );
        this.FileSystem.Mock.AllFiles.ToList().ForEach( this.Logger.WriteLine );

        Assert.Equal( expectedCount, this.FileSystem.Mock.AllFiles.Count() );
    }

    [Fact]
    public async Task ShouldReportExceptionConcurrent()
    {
        for ( var i = 0; i < 50; i++ )
        {
            var hash = Guid.NewGuid().ToString();

            bool ShouldReportIssue()
            {
                // To simulate a multi-process situation, each iteration of the test should have its own ConfigurationManager.
                var serviceProvider = this.CloneServiceCollection()
                    .AddSingleton<IConfigurationManager>( new Configuration.ConfigurationManager( this.ServiceProvider ) )
                    .BuildServiceProvider();

                var reporter = new ExceptionReporter( new TelemetryQueue( serviceProvider ), serviceProvider );

                return reporter.ShouldReportIssue( hash );
            }

            this.Logger.WriteLine( $"------------------- {i + 1} ---------------- " );
            var tasks = Enumerable.Range( 0, 10 ).Select( _ => Task.Run( ShouldReportIssue ) ).ToList();
            await Task.WhenAll( tasks );

            var trueCount = tasks.Count( t => t.Result );

            this.Logger.WriteLine( $"Tasks that managed to report the issue: {trueCount}" );

            Assert.Equal( 1, trueCount );
        }
    }

    [Fact]
    public void ShouldReportException()
    {
        var reporter = new ExceptionReporter( new TelemetryQueue( this.ServiceProvider ), this.ServiceProvider );
        Assert.False( reporter.ShouldReportException( new TaskCanceledException() ) );
        Assert.False( reporter.ShouldReportException( new OperationCanceledException() ) );
        Assert.False( reporter.ShouldReportException( new IOException() ) );
        Assert.False( reporter.ShouldReportException( new UnauthorizedAccessException() ) );
        Assert.False( reporter.ShouldReportException( new WebException() ) );
        Assert.False( reporter.ShouldReportException( new AggregateException( new IOException() ) ) );
        Assert.False( reporter.ShouldReportException( new InvalidOperationException( "", new IOException() ) ) );
        Assert.True( reporter.ShouldReportException( new InvalidOperationException( "" ) ) );
    }

    private void ReportException(
        ReportingAction exceptionReportingAction = ReportingAction.Yes,
        ReportingAction performanceReportingAction = ReportingAction.Yes,
        ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception )
        => this.ReportException( null, exceptionReportingAction, performanceReportingAction, exceptionReportingKind );

    private void ReportException(
        Exception? exception,
        ReportingAction exceptionReportingAction = ReportingAction.Yes,
        ReportingAction performanceReportingAction = ReportingAction.Yes,
        ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception )
    {
        this.ConfigurationManager!.Update<TelemetryConfiguration>(
            c => c with { ExceptionReportingAction = exceptionReportingAction, PerformanceProblemReportingAction = performanceReportingAction } );

        var reporter = new ExceptionReporter( new TelemetryQueue( this.ServiceProvider ), this.ServiceProvider );
        reporter.ReportException( exception ?? new InvalidOperationException(), exceptionReportingKind );
    }

    [Theory]
    [InlineData( ReportingAction.Yes, ReportingAction.No, ExceptionReportingKind.Exception, true )]
    [InlineData( ReportingAction.No, ReportingAction.Yes, ExceptionReportingKind.Exception, false )]
    [InlineData( ReportingAction.Ask, ReportingAction.Yes, ExceptionReportingKind.Exception, false )]
    [InlineData( ReportingAction.No, ReportingAction.Yes, ExceptionReportingKind.PerformanceProblem, true )]
    [InlineData( ReportingAction.Yes, ReportingAction.No, ExceptionReportingKind.PerformanceProblem, false )]
    [InlineData( ReportingAction.Yes, ReportingAction.Ask, ExceptionReportingKind.PerformanceProblem, false )]
    public void ExceptionsAreReportedAsConfiguredWhenTelemetryIsEnabled(
        ReportingAction exceptionReportingAction,
        ReportingAction performanceReportingAction,
        ExceptionReportingKind exceptionReportingKind,
        bool shouldReport )
    {
        this.ReportException( exceptionReportingAction, performanceReportingAction, exceptionReportingKind );

        if ( shouldReport )
        {
            this.AssertFilesCount( 1 );

            // Check that the result is valid XML.
            var xml = this.FileSystem.ReadAllText( this.FileSystem.Mock.AllFiles.Single() );
            _ = XDocument.Parse( xml );
        }
        else
        {
            this.AssertFilesCount( 0 );
        }
    }

    private void AssertReportingDisabled()
    {
        this.ReportException();
        this.AssertFilesCount( 0 );
    }

    [Fact]
    public void ExceptionsAreNotReportedWhenTelemetryIsDisabled()
    {
        this.ApplicationInfo = new TestApplicationInfo() { IsTelemetryEnabled = false };
        this.AssertReportingDisabled();
    }

    [Fact]
    public void ExceptionsAreNotReportedWhenOptOutEnvironmentVariableIsSet()
    {
        this.EnvironmentVariableProvider.Environment["METALAMA_TELEMETRY_OPT_OUT"] = "true";
        this.AssertReportingDisabled();
    }

    [Fact]
    public void ExceptionsAreNotReportedForUnattendedBuild()
    {
        this.ApplicationInfo = new TestApplicationInfo() { IsUnattendedProcess = true };
        this.AssertReportingDisabled();
    }

    [Fact]
    public void ExceptionsWithTheSameStackTraceAreReportedOnce()
    {
        var exception = new TestException( "Test", "Test" );

        this.ReportException( exception );
        this.ReportException( exception );
        this.AssertFilesCount( 1 );
    }

    [Fact]
    public void AllExceptionsWithDistinctStackTraceOfInnerExceptionAreReportedOnce()
    {
        var stackTrace1 = CreateStackFrame( "Method1", 1 );
        var stackTrace2 = CreateStackFrame( "Method2", 2 );
        var stackTrace3 = CreateStackFrame( "Method3", 3 );

        var exception1 = new TestException( "Test", stackTrace1, new TestException( "Inner", stackTrace2 ) );
        var exception2 = new TestException( "Test", stackTrace1, new TestException( "Inner", stackTrace3 ) );

        this.ReportException( exception1 );
        this.ReportException( exception2 );
        this.ReportException( exception1 );
        this.ReportException( exception2 );

        this.AssertFilesCount( 2 );
    }

    [Fact]
    public void AllExceptionsWithDistinctStackTraceOfInnerExceptionsAreReportedOnce()
    {
        var stackTrace1 = CreateStackFrame( "Method1", 1 );
        var stackTrace2 = CreateStackFrame( "Method2", 2 );
        var stackTrace3 = CreateStackFrame( "Method3", 3 );

        var innerException1 = new TestException( "Inner1", stackTrace1 );
        var innerException2 = new TestException( "Inner2", stackTrace2 );
        var innerException3 = new TestException( "Inner3", stackTrace3 );

        var exception1 = CreateAggregateException( "Test", [innerException1, innerException2] );
        var exception2 = CreateAggregateException( "Test", [innerException1, innerException3] );

        this.ReportException( exception1 );
        this.ReportException( exception2 );
        this.ReportException( exception1 );
        this.ReportException( exception2 );

        this.AssertFilesCount( 2 );
    }

    [Fact]
    public void SubsequentUserStackFramesAreIgnored()
    {
        var stackTrace1 = CreateStackTrace( [("Method1", 1), ("#user", 2), ("Method2", 4)] );
        var stackTrace2 = CreateStackTrace( [("Method1", 1), ("#user", 2), ("#user", 3), ("Method2", 4)] );

        var exception1 = new TestException( "Test", stackTrace1 );
        var exception2 = new TestException( "Test", stackTrace2 );

        this.ReportException( exception1 );
        this.ReportException( exception2 );

        this.AssertFilesCount( 1 );
    }
    
    [Fact]
    public void StackFramesAfterUserStackFramesAreNotIgnored()
    {
        var stackTrace1 = CreateStackTrace( [("Method1", 1), ("#user", 2), ("Method2", 4), ("Method3", 5)] );
        var stackTrace2 = CreateStackTrace( [("Method1", 1), ("#user", 2), ("#user", 3), ("Method2", 4)] );

        var exception1 = new TestException( "Test", stackTrace1 );
        var exception2 = new TestException( "Test", stackTrace2 );

        this.ReportException( exception1 );
        this.ReportException( exception2 );

        this.AssertFilesCount( 2 );
    }
    
    [Fact]
    public void MultipleUserStackTraceSectionsAreNotIgnored()
    {
        var stackTrace1 = CreateStackTrace( [("Method1", 1), ("#user", 2), ("Method2", 3), ("#user", 4), ("Method3", 5)] );
        var stackTrace2 = CreateStackTrace( [("Method1", 1), ("#user", 2), ("Method2", 3), ("Method3", 5)] );

        var exception1 = new TestException( "Test", stackTrace1 );
        var exception2 = new TestException( "Test", stackTrace2 );

        this.ReportException( exception1 );
        this.ReportException( exception2 );

        this.AssertFilesCount( 2 );
    }
}