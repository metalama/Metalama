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
        public string? ContentToReturn { get; set; }

        public string? RequestedContentReport { get; private set; }

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

        public string? TryGetReportContent( string reportFileName )
        {
            this.RequestedContentReport = reportFileName;

            return this.ContentToReturn;
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
    public void OnGetLoadsTheScrubbedReportForReview()
    {
        var reporter = new FakeExceptionReporter { ContentToReturn = "<ErrorReport />" };
        var model = this.CreateModel( reporter );

        model.OnGet( "exception-abc.xml", "Exception" );

        Assert.Equal( "exception-abc.xml", reporter.RequestedContentReport );
        Assert.Equal( "<ErrorReport />", model.ReportContent );
        Assert.Equal( TelemetryScenario.Exception, model.Scenario );
        Assert.False( model.IsReported );
    }

    [Theory]
    [InlineData( "Performance", TelemetryScenario.Performance )]
    [InlineData( "Exception", TelemetryScenario.Exception )]
    [InlineData( null, TelemetryScenario.Exception )]
    public void ScenarioIsDerivedFromCategory( string? category, TelemetryScenario expected )
    {
        var model = this.CreateModel( new FakeExceptionReporter() );
        model.Category = category;

        Assert.Equal( expected, model.Scenario );
    }

    [Fact]
    public void OnPostReportSendsTheReportWithoutEnablingAutoReport()
    {
        this.TelemetryConfigurationService.Initialize();

        var reporter = new FakeExceptionReporter();
        var model = this.CreateModel( reporter );
        model.Report = "exception-abc.xml";
        model.Category = "Exception";
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

        var reporter = new FakeExceptionReporter();
        var model = this.CreateModel( reporter );
        model.Report = "perf-xyz.xml";
        model.Category = "Performance";
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
