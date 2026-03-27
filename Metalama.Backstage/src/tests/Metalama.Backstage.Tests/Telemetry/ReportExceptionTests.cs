// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
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

public sealed class ReportExceptionTests : TestsBase
{
    public ReportExceptionTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } ) { }

    protected override void OnAfterServicesCreated( Services services )
    {
        base.OnAfterServicesCreated( services );

        services.ConfigurationManager!.Update<TelemetryConfiguration>(
            c => c with { ExceptionReportingAction = ReportingAction.Yes, PerformanceProblemReportingAction = ReportingAction.Yes } );
    }

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
        Assert.False( ExceptionClassifier.Classify( new TaskCanceledException() ).IsError );
        Assert.False( ExceptionClassifier.Classify( new OperationCanceledException() ).IsError );
        Assert.False( ExceptionClassifier.Classify( new IOException() ).IsError );
        Assert.False( ExceptionClassifier.Classify( new UnauthorizedAccessException() ).IsError );
        Assert.False( ExceptionClassifier.Classify( new WebException() ).IsError );
        Assert.False( ExceptionClassifier.Classify( new AggregateException( new IOException() ) ).IsError );
        Assert.False( ExceptionClassifier.Classify( new InvalidOperationException( "", new IOException() ) ).IsError );
        Assert.True( ExceptionClassifier.Classify( new InvalidOperationException( "" ) ).IsError );
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
    [InlineData( ReportingAction.No, ReportingAction.Yes, ExceptionReportingKind.PerformanceProblem, true )]
    [InlineData( ReportingAction.Yes, ReportingAction.No, ExceptionReportingKind.PerformanceProblem, false )]
    [InlineData( ReportingAction.Yes, ReportingAction.Default, ExceptionReportingKind.PerformanceProblem, true )]
    [InlineData( ReportingAction.Default, ReportingAction.Yes, ExceptionReportingKind.Exception, true )]
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

    [Fact]
    public async Task AsyncExceptionReportContainsCompleteStackTrace()
    {
        // Simulate an exception thrown in a deep async method and propagated through await chain,
        // as happens in VS Extension entry points (CodeLens, Preview, AspectExplorer).
        Exception? caughtException = null;

        try
        {
            await OuterAsyncMethod();
        }
        catch ( Exception e )
        {
            caughtException = e;
        }

        Assert.NotNull( caughtException );

        this.ReportException( caughtException );
        this.AssertFilesCount( 1 );

        var xml = this.FileSystem.ReadAllText( this.FileSystem.Mock.AllFiles.Single() );
        this.Logger.WriteLine( "XML report:" );
        this.Logger.WriteLine( xml );

        var doc = XDocument.Parse( xml );
        var stackTraceElement = doc.Descendants( "StackTrace" ).First();
        var stackTrace = stackTraceElement.Value;

        this.Logger.WriteLine( "StackTrace element content:" );
        this.Logger.WriteLine( stackTrace );

        // The stack trace should contain the original throw site (ThrowingMethod).
        Assert.Contains( "ThrowingMethod", stackTrace, StringComparison.Ordinal );

        // The stack trace should contain the intermediate async method.
        Assert.Contains( "InnerAsyncMethod", stackTrace, StringComparison.Ordinal );

        // The stack trace should contain the outer async method.
        Assert.Contains( "OuterAsyncMethod", stackTrace, StringComparison.Ordinal );

        return;

        static async Task ThrowingMethod()
        {
            await Task.Yield();

            throw new InvalidOperationException( "Test async exception" );
        }

        static async Task InnerAsyncMethod()
        {
            await ThrowingMethod();
        }

        static async Task OuterAsyncMethod()
        {
            await InnerAsyncMethod();
        }
    }

    [Fact]
    public void ExceptionReportContainsReportingCallStack()
    {
        // The crash report should include the call stack at the point where the exception
        // was reported, providing context about the entry point that caught the exception.
        var exception = new TestException( "Test", CreateStackFrame( "DeepMethod", 1 ) );

        this.ReportException( exception );
        this.AssertFilesCount( 1 );

        var xml = this.FileSystem.ReadAllText( this.FileSystem.Mock.AllFiles.Single() );
        this.Logger.WriteLine( "XML report:" );
        this.Logger.WriteLine( xml );

        var doc = XDocument.Parse( xml );

        // The report should contain a ReportingCallStack element that shows where ReportException was called from.
        var reportingCallStack = doc.Descendants( "ReportingCallStack" ).FirstOrDefault();
        Assert.NotNull( reportingCallStack );
        Assert.NotEmpty( reportingCallStack.Value );

        this.Logger.WriteLine( "ReportingCallStack:" );
        this.Logger.WriteLine( reportingCallStack.Value );
    }
}