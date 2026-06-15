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
        public LocalExceptionReport? ReportToReturn { get; set; }

        public string? RequestedReport { get; private set; }

        public string? SentReport { get; private set; }

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

        public bool TryGetReport( string reportFileName, [NotNullWhen( true )] out LocalExceptionReport? report )
        {
            this.RequestedReport = reportFileName;
            report = this.ReportToReturn;

            return report != null;
        }

        public bool SendReport( string reportFileName )
        {
            this.SentReport = reportFileName;

            return true;
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
            ReportToReturn = new LocalExceptionReport( "<ErrorReport />", "<ErrorReport local=\"true\" />", TelemetryScenario.Exception )
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
        var reporter = new FakeExceptionReporter { ReportToReturn = new LocalExceptionReport( "<r/>", null, category ) };
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
            ReportToReturn = new LocalExceptionReport( "<r/>", null, TelemetryScenario.Exception )
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
            ReportToReturn = new LocalExceptionReport( "<r/>", null, TelemetryScenario.Performance )
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
}
