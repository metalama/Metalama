// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Pages;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.Testing;
using System;
using System.Diagnostics.CodeAnalysis;
using Xunit;
using Xunit.Abstractions;
using IConfigurationManager = Metalama.Backstage.Configuration.IConfigurationManager;

namespace Metalama.Backstage.Worker.Tests;

public sealed class ExceptionReportPageModelTests : TestsBase
{
    public ExceptionReportPageModelTests( ITestOutputHelper logger )
        : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } ) { }

    // A fake IExceptionReporter so the page-model orchestration can be tested without the (internal) ExceptionReporter.
    private sealed class FakeExceptionReporter : IExceptionReporter
    {
        public CapturedExceptionReport? ReportToReturn { get; set; }

        public string? RequestedReport { get; private set; }

        public string? SentReport { get; private set; }

        public bool SendReportResult { get; set; } = true;

        public void ReportException(
            Exception exception,
            ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
            string? localReportPath = null,
            IExceptionAdapter? exceptionAdapter = null ) { }

        public void ReportException(
            ClassifiedException classifiedException,
            ExceptionReportingKind exceptionReportingKind = ExceptionReportingKind.Exception,
            string? localReportPath = null,
            IExceptionAdapter? exceptionAdapter = null ) { }

        public bool TryGetReport( string reportFileName, [NotNullWhen( true )] out CapturedExceptionReport? report )
        {
            this.RequestedReport = reportFileName;
            report = this.ReportToReturn;

            return report != null;
        }

        public bool SendReport( string reportFileName )
        {
            this.SentReport = reportFileName;

            return this.SendReportResult;
        }
    }

    private ExceptionReportPageModel CreateModel( IExceptionReporter exceptionReporter )
        => new(
            exceptionReporter,
            this.ServiceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>(),
            this.ServiceProvider.GetRequiredBackstageService<IConfigurationManager>() );

    private ReportingAction GetReportingAction( TelemetryScenario scenario )
        => this.ServiceProvider.GetRequiredBackstageService<IConfigurationManager>().Get<TelemetryConfiguration>().GetReportingAction( scenario );

    [Fact]
    public void OnGetLoadsBothReportRenderingsForReview()
    {
        var reporter = new FakeExceptionReporter
        {
            ReportToReturn = new CapturedExceptionReport( "<ErrorReport />", "<ErrorReport local=\"true\" />", TelemetryScenario.Exception )
        };

        var model = this.CreateModel( reporter );

        model.OnGet( "exception-abc.xml" );

        Assert.Equal( "exception-abc.xml", reporter.RequestedReport );
        Assert.Equal( "<ErrorReport />", model.ScrubbedContent );
        Assert.Equal( "<ErrorReport local=\"true\" />", model.LocalContent );
        Assert.True( model.HasReport );

        // The category is read from the report itself, not passed as a parameter.
        Assert.Equal( TelemetryScenario.Exception, model.Scenario );
        Assert.False( model.IsReported );
    }

    [Fact]
    public void OnGetMarksAnAlreadyQueuedReportAsSent()
    {
        // #1674: a report that was already auto-sent (and therefore is queued) opens in a read-only "already sent" state:
        // the renderings are still loaded for transparency, but the page must not offer the Report button again.
        var reporter = new FakeExceptionReporter
        {
            ReportToReturn = new CapturedExceptionReport(
                "<ErrorReport />",
                "<ErrorReport local=\"true\" />",
                TelemetryScenario.Exception,
                IsQueued: true )
        };

        var model = this.CreateModel( reporter );

        model.OnGet( "exception-abc.xml" );

        Assert.True( model.IsAlreadySent );
        Assert.True( model.HasReport );
        Assert.Equal( "<ErrorReport local=\"true\" />", model.LocalContent );

        // It is not the post-submit "thank you" state; the content is shown without the Report form.
        Assert.False( model.IsReported );
    }

    [Theory]
    [InlineData( true, ReportingAction.Yes )]
    [InlineData( false, ReportingAction.No )]
    public void OnPostUpdateAutoReportTogglesTheCategoryWithoutResending( bool autoReport, ReportingAction expectedAction )
    {
        // #1674: on an already-sent report the auto-report checkbox stays available so the user can turn auto-reporting
        // on or off for the category. This only updates the setting — it does not (re-)send the report.
        this.TelemetryConfigurationService.Initialize();

        var reporter = new FakeExceptionReporter
        {
            ReportToReturn = new CapturedExceptionReport( "<r/>", "<r local/>", TelemetryScenario.Exception, IsQueued: true )
        };

        var model = this.CreateModel( reporter );
        model.Report = "exception-abc.xml";
        model.AutoReport = autoReport;

        model.OnPostUpdateAutoReport();

        Assert.Equal( expectedAction, this.GetReportingAction( TelemetryScenario.Exception ) );

        // The page still renders as already sent, with the report content shown, and never as the "report queued" state.
        Assert.True( model.IsAlreadySent );
        Assert.True( model.HasReport );
        Assert.False( model.IsReported );

        // The report was not (re-)sent.
        Assert.Null( reporter.SentReport );
    }

    [Fact]
    public void OnGetWithUnknownReportLeavesPageEmpty()
    {
        var model = this.CreateModel( new FakeExceptionReporter() );

        model.OnGet( "does-not-exist.xml" );

        Assert.False( model.HasReport );
        Assert.Null( model.ScrubbedContent );
    }

    [Theory]
    [InlineData( TelemetryScenario.Performance )]
    [InlineData( TelemetryScenario.Exception )]
    public void ScenarioIsReadFromTheReport( TelemetryScenario category )
    {
        var reporter = new FakeExceptionReporter { ReportToReturn = new CapturedExceptionReport( "<r/>", null, category ) };
        var model = this.CreateModel( reporter );

        model.OnGet( "report.xml" );

        Assert.Equal( category, model.Scenario );
    }

    [Fact]
    public void OnPostReportSendsTheReportWithoutEnablingAutoReport()
    {
        this.TelemetryConfigurationService.Initialize();

        var reporter = new FakeExceptionReporter
        {
            ReportToReturn = new CapturedExceptionReport( "<r/>", null, TelemetryScenario.Exception )
        };

        var model = this.CreateModel( reporter );
        model.Report = "exception-abc.xml";
        model.AutoReport = false;

        model.OnPostReport();

        Assert.True( model.IsReported );
        Assert.Equal( "exception-abc.xml", reporter.SentReport );

        // The category stays review-first because the checkbox was not ticked.
        Assert.Equal( ReportingAction.Default, this.GetReportingAction( TelemetryScenario.Exception ) );
    }

    [Fact]
    public void OnPostReportWithAutoReportEnablesOnlyThatCategory()
    {
        this.TelemetryConfigurationService.Initialize();

        var reporter = new FakeExceptionReporter
        {
            ReportToReturn = new CapturedExceptionReport( "<r/>", null, TelemetryScenario.Performance )
        };

        var model = this.CreateModel( reporter );
        model.Report = "perf-xyz.xml";
        model.AutoReport = true;

        model.OnPostReport();

        Assert.Equal( "perf-xyz.xml", reporter.SentReport );

        // Ticking "automatically report all performance warnings" enables auto-send for performance only.
        Assert.Equal( ReportingAction.Yes, this.GetReportingAction( TelemetryScenario.Performance ) );

        // Exceptions and usage telemetry are independent and unchanged.
        Assert.Equal( ReportingAction.Default, this.GetReportingAction( TelemetryScenario.Exception ) );
        Assert.Equal( ReportingAction.Yes, this.GetReportingAction( TelemetryScenario.Usage ) );
    }

    [Fact]
    public void OnPostReportDoesNotShowQueuedWhenSendFails()
    {
        // Copilot review: the page must only show "Report queued" when the report was actually enqueued. If SendReport
        // fails (e.g. the report file was removed), IsReported stays false so the page does not falsely claim success.
        this.TelemetryConfigurationService.Initialize();

        var reporter = new FakeExceptionReporter
        {
            ReportToReturn = new CapturedExceptionReport( "<r/>", null, TelemetryScenario.Exception ), SendReportResult = false
        };

        var model = this.CreateModel( reporter );
        model.Report = "exception-abc.xml";

        model.OnPostReport();

        Assert.Equal( "exception-abc.xml", reporter.SentReport );
        Assert.False( model.IsReported );

        // The report content is still loaded, so the page re-renders the report rather than a blank form.
        Assert.True( model.HasReport );
    }
}
