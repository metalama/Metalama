// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using Metalama.Backstage.UserInterface.Toasts;
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
            c => c with { ExceptionConsent = TelemetryConsent.Yes, PerformanceProblemConsent = TelemetryConsent.Yes } );

        this.UserDeviceDetection.IsInteractiveDevice = true;
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

        // The full local rendering ('.local.xml') is a review-only sibling of the scrubbed upload payload; it is never
        // uploaded, so it does not count as a captured report. See #1674.
        var reportFiles = this.FileSystem.Mock.AllFiles.Count( f => !f.EndsWith( ".local.xml", StringComparison.Ordinal ) );

        Assert.Equal( expectedCount, reportFiles );
    }

    // The scrubbed upload payload is the '.xml' file that is not the '.local.xml' sibling.
    private string GetScrubbedReportFile()
        => this.FileSystem.Mock.AllFiles.Single( f => f.EndsWith( ".xml", StringComparison.Ordinal ) && !f.EndsWith( ".local.xml", StringComparison.Ordinal ) );

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

    // Sets the reporting action of the category matching <paramref name="kind"/> and opts the other category out
    // (ReportingAction.No), so a test exercises exactly one category at a time and proves the two are independent.
    // Tests that need both categories enabled rely instead on the both-Yes baseline set in OnAfterServicesCreated.
    private void ConfigureExceptionReporting( ExceptionReportingKind kind, TelemetryConsent action )
        => this.ConfigurationManager!.Update<TelemetryConfiguration>(
            c => kind == ExceptionReportingKind.Exception
                ? c with { ExceptionConsent = action, PerformanceProblemConsent = TelemetryConsent.No }
                : c with { ExceptionConsent = TelemetryConsent.No, PerformanceProblemConsent = action } );

    // Reports an exception (a default InvalidOperationException when none is given) through a fresh reporter, using
    // whatever reporting configuration is currently in effect. Combine with ConfigureExceptionReporting to exercise a
    // specific action.
    private void RecordException( Exception? exception = null, ExceptionReportingKind kind = ExceptionReportingKind.Exception )
    {
        var reporter = new ExceptionReporter( new TelemetryQueue( this.ServiceProvider ), this.ServiceProvider );

        // Capture now receives the policy-resolved action explicitly (in production the telemetry context does this). We
        // resolve it the same way the default policy does — through the configuration service. See #1701.
        var scenario = kind == ExceptionReportingKind.Exception ? TelemetryScenario.Exception : TelemetryScenario.Performance;
        var action = this.ServiceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>().GetEffectiveConsent( scenario );

        reporter.Capture( ExceptionClassifier.Classify( exception ?? new InvalidOperationException() ), kind, action, writeLocalReport: true, adapter: null );
    }

    [Theory]

    // #1674, #1701: For an ACTIVE category, capture is decoupled from sending — a scrubbed report is captured for both
    // Default (ASK) and Yes, and the action only decides whether it is additionally auto-sent (moved to the upload
    // queue). The No case (not captured nor asked) is covered by ReportIsNotCapturedNorToastShownWhenCategoryIsOptedOut.
    // 'shouldEnqueue' is for the kind being reported; ConfigureExceptionReporting opts the other kind out to prove independence.
    [InlineData( TelemetryConsent.Yes, ExceptionReportingKind.Exception, true )]
    [InlineData( TelemetryConsent.Default, ExceptionReportingKind.Exception, false )]
    [InlineData( TelemetryConsent.Yes, ExceptionReportingKind.PerformanceProblem, true )]
    [InlineData( TelemetryConsent.Default, ExceptionReportingKind.PerformanceProblem, false )]
    public void ReportIsCapturedForActiveCategoryAndEnqueuedOnlyWhenYes(
        TelemetryConsent telemetryConsent,
        ExceptionReportingKind exceptionReportingKind,
        bool shouldEnqueue )
    {
        this.ConfigureExceptionReporting( exceptionReportingKind, telemetryConsent );
        this.RecordException( kind: exceptionReportingKind );

        // A scrubbed report is captured in all cases (it is valid XML).
        var scrubbed = this.GetScrubbedReportFile();
        _ = XDocument.Parse( this.FileSystem.ReadAllText( scrubbed ) );

        if ( shouldEnqueue )
        {
            Assert.Contains( Path.Combine( "Telemetry", "UploadQueue" ), scrubbed, StringComparison.Ordinal );
        }
        else
        {
            Assert.Contains( Path.Combine( "Telemetry", "Exceptions" ), scrubbed, StringComparison.Ordinal );

            Assert.DoesNotContain(
                this.FileSystem.Mock.AllFiles,
                f => f.Contains( Path.Combine( "Telemetry", "UploadQueue" ), StringComparison.Ordinal ) );
        }
    }

    [Theory]
    [InlineData( ExceptionReportingKind.Exception )]
    [InlineData( ExceptionReportingKind.PerformanceProblem )]
    public void ReportIsNotCapturedNorToastShownWhenCategoryIsOptedOut( ExceptionReportingKind exceptionReportingKind )
    {
        // #1701: When the category is explicitly opted out (ReportingAction.No), the report is NOT even captured and no
        // review toast is shown — the user has chosen not to be asked. (The local crash report is independent of
        // telemetry and is written separately, by LocalExceptionReporter.)
        this.ConfigureExceptionReporting( exceptionReportingKind, TelemetryConsent.No );
        this.RecordException( kind: exceptionReportingKind );

        // No telemetry report (neither the scrubbed payload nor the local rendering) is captured.
        Assert.DoesNotContain( this.FileSystem.Mock.AllFiles, f => f.EndsWith( ".xml", StringComparison.Ordinal ) );

        // And the user is not asked.
        Assert.Empty( this.UserInterface.Notifications );
    }

    [Theory]
    [InlineData( ExceptionReportingKind.Exception )]
    [InlineData( ExceptionReportingKind.PerformanceProblem )]
    public void AutoSendCategoryEnqueuesReportForUpload( ExceptionReportingKind exceptionReportingKind )
    {
        // #1674: When the category is explicitly opted in (ReportingAction.Yes), the captured report is auto-sent,
        // i.e. the scrubbed payload is moved to the telemetry upload queue. The full local rendering is still kept
        // alongside (under Telemetry\Exceptions) so the review page can show it, and is never uploaded.
        this.ConfigureExceptionReporting( exceptionReportingKind, TelemetryConsent.Yes );
        this.RecordException( kind: exceptionReportingKind );

        var scrubbed = this.GetScrubbedReportFile();
        Assert.Contains( Path.Combine( "Telemetry", "UploadQueue" ), scrubbed, StringComparison.Ordinal );

        var localRendering = Assert.Single( this.FileSystem.Mock.AllFiles, f => f.EndsWith( ".local.xml", StringComparison.Ordinal ) );
        Assert.Contains( Path.Combine( "Telemetry", "Exceptions" ), localRendering, StringComparison.Ordinal );
        Assert.DoesNotContain( Path.Combine( "Telemetry", "UploadQueue" ), localRendering, StringComparison.Ordinal );
    }

    [Theory]
    [InlineData( ExceptionReportingKind.Exception )]
    [InlineData( ExceptionReportingKind.PerformanceProblem )]
    public void ReviewFirstCategoryCapturesReportLocallyWithoutEnqueuing( ExceptionReportingKind exceptionReportingKind )
    {
        // #1674: Capture is decoupled from sending. When the category is review-first (ReportingAction.Default), the
        // scrubbed report is captured locally under Telemetry\Exceptions but NOT enqueued for upload, so it stays
        // under the user's control until they review and send it from the worker page / CLI.
        this.ConfigureExceptionReporting( exceptionReportingKind, TelemetryConsent.Default );
        this.RecordException( kind: exceptionReportingKind );

        // The scrubbed upload payload stays under Telemetry\Exceptions and is NOT enqueued for upload.
        var scrubbed = this.GetScrubbedReportFile();
        Assert.Contains( Path.Combine( "Telemetry", "Exceptions" ), scrubbed, StringComparison.Ordinal );
        Assert.DoesNotContain( Path.Combine( "Telemetry", "UploadQueue" ), scrubbed, StringComparison.Ordinal );

        // A full, unscrubbed local rendering is captured alongside for side-by-side review (#1674).
        var localRendering = Assert.Single( this.FileSystem.Mock.AllFiles, f => f.EndsWith( ".local.xml", StringComparison.Ordinal ) );
        Assert.Contains( Path.Combine( "Telemetry", "Exceptions" ), localRendering, StringComparison.Ordinal );
    }

    [Theory]
    [InlineData( ExceptionReportingKind.Exception )]
    [InlineData( ExceptionReportingKind.PerformanceProblem )]
    public void ReviewFirstCaptureWritesScrubbedAndFullLocalRenderings( ExceptionReportingKind exceptionReportingKind )
    {
        // #1674: A review-first capture writes both the scrubbed upload payload (.xml) and the full, unscrubbed local
        // rendering (.local.xml), so the review page can show them side by side. The scrubber redacts the user-specific
        // path from the upload payload, while the local rendering keeps it so the user sees exactly what was removed.
        var exception = new InvalidOperationException( @"Failed reading C:\Users\johndoe\secret.txt" );

        this.ConfigureExceptionReporting( exceptionReportingKind, TelemetryConsent.Default );
        this.RecordException( exception, exceptionReportingKind );

        var reporter = new ExceptionReporter( new TelemetryQueue( this.ServiceProvider ), this.ServiceProvider );

        Assert.True( reporter.TryGetReport( Path.GetFileName( this.GetScrubbedReportFile() ), out var report ) );
        Assert.NotNull( report!.LocalContent );

        // The upload payload is scrubbed: the user-specific path is redacted.
        Assert.DoesNotContain( "johndoe", report.ScrubbedContent, StringComparison.Ordinal );

        // The local rendering keeps the full detail.
        Assert.Contains( "johndoe", report.LocalContent!, StringComparison.Ordinal );

        // Both are valid XML and self-contained (the category is stored in the report).
        _ = XDocument.Parse( report.ScrubbedContent );
        _ = XDocument.Parse( report.LocalContent! );

        var expectedCategory = exceptionReportingKind == ExceptionReportingKind.Exception ? TelemetryScenario.Exception : TelemetryScenario.Performance;
        Assert.Equal( expectedCategory, report.Category );
    }

    [Theory]
    [InlineData( ExceptionReportingKind.Exception )]
    [InlineData( ExceptionReportingKind.PerformanceProblem )]
    public void ToastOpensReviewPage( ExceptionReportingKind exceptionReportingKind )
    {
        this.TelemetryConfigurationService.SetConsent( TelemetryConsent.Default );

        // #1674: When a report is captured, a toast is shown whose action opens the review page for that exact report
        // (instead of opening the raw report file). The toast Uri carries only the bare report file name (token-safe);
        // the category is stored in the report itself, and the desktop command builds the review-page path from the id.
        this.RecordException( kind: exceptionReportingKind );

        var toast = Assert.Single( this.UserInterface.Notifications );
        Assert.Equal( ToastNotificationKinds.Exception.Name, toast.Kind.Name );
        Assert.NotNull( toast.Uri );
        Assert.StartsWith( "exception-", toast.Uri, StringComparison.Ordinal );
        Assert.EndsWith( ".xml", toast.Uri, StringComparison.Ordinal );

        // The Uri must stay a single space-free token because the desktop toast activation argument is split on spaces.
        Assert.DoesNotContain( ' ', toast.Uri! );

        // The Uri must not carry a page path or query string — only the report id.
        Assert.DoesNotContain( '?', toast.Uri! );

        // The report referenced by the toast must be the captured report on disk.
        var reportFileName = Path.GetFileName( this.GetScrubbedReportFile() );
        Assert.Equal( reportFileName, toast.Uri );
    }

    [Fact]
    public void SendReportEnqueuesCapturedReviewFirstReport()
    {
        // #1674: A review-first report stays local until the user reviews and sends it. SendReport (invoked from the
        // worker review page's Report button) enqueues that exact report for upload.
        this.ConfigureExceptionReporting( ExceptionReportingKind.Exception, TelemetryConsent.Default );
        this.RecordException();

        var captured = this.GetScrubbedReportFile();
        Assert.Contains( Path.Combine( "Telemetry", "Exceptions" ), captured, StringComparison.Ordinal );

        var reporter = new ExceptionReporter( new TelemetryQueue( this.ServiceProvider ), this.ServiceProvider );
        var reportFileName = Path.GetFileName( captured );

        // The exact scrubbed payload (and the full local rendering) can be read back for review.
        Assert.True( reporter.TryGetReport( reportFileName, out var report ) );
        _ = XDocument.Parse( report!.ScrubbedContent );

        // Sending moves the scrubbed report to the upload queue.
        Assert.True( reporter.SendReport( reportFileName ) );

        var enqueued = Assert.Single( this.FileSystem.Mock.AllFiles, f => f.Contains( Path.Combine( "Telemetry", "UploadQueue" ), StringComparison.Ordinal ) );
        Assert.EndsWith( ".xml", enqueued, StringComparison.Ordinal );
        Assert.DoesNotContain( ".local.xml", enqueued, StringComparison.Ordinal );
    }

    [Fact]
    public void TryGetReportFindsAutoSentReportInUploadQueue()
    {
        // Copilot: an auto-sent (Yes) report is moved to the upload queue. Clicking its toast must still show it (the
        // toast says "review what was reported"), so report lookup resolves reports in the upload queue too.
        this.ConfigureExceptionReporting( ExceptionReportingKind.Exception, TelemetryConsent.Yes );
        this.RecordException();

        var enqueued = this.GetScrubbedReportFile();
        Assert.Contains( Path.Combine( "Telemetry", "UploadQueue" ), enqueued, StringComparison.Ordinal );

        var reporter = new ExceptionReporter( new TelemetryQueue( this.ServiceProvider ), this.ServiceProvider );
        Assert.True( reporter.TryGetReport( Path.GetFileName( enqueued ), out var report ) );
        _ = XDocument.Parse( report!.ScrubbedContent );
    }

    [Fact]
    public void SendReportIsIdempotentForAlreadyQueuedReport()
    {
        // Copilot: an already-queued report (auto-sent, or sent on a previous click) must not be treated as a failure;
        // SendReport returns true without moving anything, so the worker page's Report button stays reliable.
        this.ConfigureExceptionReporting( ExceptionReportingKind.Exception, TelemetryConsent.Yes );
        this.RecordException();
        var enqueued = Path.GetFileName( this.GetScrubbedReportFile() );

        var reporter = new ExceptionReporter( new TelemetryQueue( this.ServiceProvider ), this.ServiceProvider );
        Assert.True( reporter.SendReport( enqueued ) );

        // Still exactly one scrubbed report, still in the upload queue (no duplicate, no move error).
        var file = this.GetScrubbedReportFile();
        Assert.Contains( Path.Combine( "Telemetry", "UploadQueue" ), file, StringComparison.Ordinal );
    }

    [Fact]
    public void SendAndReadRejectTheFullLocalRendering()
    {
        // Copilot: the full, unscrubbed local rendering ('.local.xml') must never be uploadable. TryGetReport/SendReport
        // reject it even though it is a bare file name that exists on disk.
        this.ConfigureExceptionReporting( ExceptionReportingKind.Exception, TelemetryConsent.Default );
        this.RecordException();

        var localRendering = Path.GetFileName( Assert.Single( this.FileSystem.Mock.AllFiles, f => f.EndsWith( ".local.xml", StringComparison.Ordinal ) ) );

        var reporter = new ExceptionReporter( new TelemetryQueue( this.ServiceProvider ), this.ServiceProvider );

        Assert.False( reporter.TryGetReport( localRendering, out var report ) );
        Assert.Null( report );
        Assert.False( reporter.SendReport( localRendering ) );

        // The local rendering was not enqueued for upload.
        Assert.DoesNotContain(
            this.FileSystem.Mock.AllFiles,
            f => f.Contains( Path.Combine( "Telemetry", "UploadQueue" ), StringComparison.Ordinal ) );
    }

    [Theory]
    [InlineData( "../telemetry.json" )]
    [InlineData( "sub/report.xml" )]
    [InlineData( "sub\\report.xml" )]
    [InlineData( "" )]
    [InlineData( "does-not-exist.xml" )]
    [InlineData( "exception-abc.local.xml" )]
    public void SendAndReadRejectInvalidOrMissingReportNames( string reportFileName )
    {
        // Guard against path traversal and missing files: a review page request must never read or send an arbitrary
        // file outside the local exceptions directory.
        var reporter = new ExceptionReporter( new TelemetryQueue( this.ServiceProvider ), this.ServiceProvider );

        Assert.False( reporter.TryGetReport( reportFileName, out var report ) );
        Assert.Null( report );
        Assert.False( reporter.SendReport( reportFileName ) );
    }

    private void AssertReportingDisabled()
    {
        this.RecordException();
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
        this.EnvironmentVariableProvider.Environment[TelemetryConfiguration.OptOutEnvironmentVariableName] = "true";
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

        this.RecordException( exception );
        this.RecordException( exception );
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

        this.RecordException( exception1 );
        this.RecordException( exception2 );
        this.RecordException( exception1 );
        this.RecordException( exception2 );

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

        this.RecordException( exception1 );
        this.RecordException( exception2 );
        this.RecordException( exception1 );
        this.RecordException( exception2 );

        this.AssertFilesCount( 2 );
    }

    [Fact]
    public void SubsequentUserStackFramesAreIgnored()
    {
        var stackTrace1 = CreateStackTrace( [("Method1", 1), ("#user", 2), ("Method2", 4)] );
        var stackTrace2 = CreateStackTrace( [("Method1", 1), ("#user", 2), ("#user", 3), ("Method2", 4)] );

        var exception1 = new TestException( "Test", stackTrace1 );
        var exception2 = new TestException( "Test", stackTrace2 );

        this.RecordException( exception1 );
        this.RecordException( exception2 );

        this.AssertFilesCount( 1 );
    }

    [Fact]
    public void StackFramesAfterUserStackFramesAreNotIgnored()
    {
        var stackTrace1 = CreateStackTrace( [("Method1", 1), ("#user", 2), ("Method2", 4), ("Method3", 5)] );
        var stackTrace2 = CreateStackTrace( [("Method1", 1), ("#user", 2), ("#user", 3), ("Method2", 4)] );

        var exception1 = new TestException( "Test", stackTrace1 );
        var exception2 = new TestException( "Test", stackTrace2 );

        this.RecordException( exception1 );
        this.RecordException( exception2 );

        this.AssertFilesCount( 2 );
    }

    [Fact]
    public void MultipleUserStackTraceSectionsAreNotIgnored()
    {
        var stackTrace1 = CreateStackTrace( [("Method1", 1), ("#user", 2), ("Method2", 3), ("#user", 4), ("Method3", 5)] );
        var stackTrace2 = CreateStackTrace( [("Method1", 1), ("#user", 2), ("Method2", 3), ("Method3", 5)] );

        var exception1 = new TestException( "Test", stackTrace1 );
        var exception2 = new TestException( "Test", stackTrace2 );

        this.RecordException( exception1 );
        this.RecordException( exception2 );

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

        this.RecordException( caughtException );
        this.AssertFilesCount( 1 );

        var xml = this.FileSystem.ReadAllText( this.GetScrubbedReportFile() );
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
    public void ClientIdIsNotRawDeviceGuid()
    {
        // Regression test for #1668: the exception report (a first-party / bits-bound channel) must NOT
        // transmit the raw DeviceId GUID. The <ClientId> element must carry an anonymized hash
        // (keyed by the first-party-only ExceptionReportingSalt), not a parseable GUID.
        this.RecordException();
        this.AssertFilesCount( 1 );

        var xml = this.FileSystem.ReadAllText( this.GetScrubbedReportFile() );
        this.Logger.WriteLine( "XML report:" );
        this.Logger.WriteLine( xml );

        var doc = XDocument.Parse( xml );
        var clientId = doc.Descendants( "ClientId" ).FirstOrDefault();
        Assert.NotNull( clientId );
        Assert.NotEmpty( clientId.Value );

        // The raw DeviceId GUID (the rotation seed from which the Matomo hash is derivable) must never
        // leave the machine. A valid GUID format here means the raw identifier is being transmitted.
        Assert.False(
            Guid.TryParse( clientId.Value, out _ ),
            $"ClientId '{clientId.Value}' is a raw GUID; it must be an anonymized hash (#1668)." );
    }

    [Fact]
    public void ExceptionReportContainsReportingCallStack()
    {
        // The crash report should include the call stack at the point where the exception
        // was reported, providing context about the entry point that caught the exception.
        var exception = new TestException( "Test", CreateStackFrame( "DeepMethod", 1 ) );

        this.RecordException( exception );
        this.AssertFilesCount( 1 );

        var xml = this.FileSystem.ReadAllText( this.GetScrubbedReportFile() );
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