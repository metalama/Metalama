// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Telemetry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IConfigurationManager = Metalama.Backstage.Configuration.IConfigurationManager;

namespace Metalama.Backstage.Pages;

#pragma warning disable SA1649

// Lets the user review a locally-captured exception/performance report before it is sent. The page shows two renderings
// of the same report — the full local report and the exact scrubbed payload that would be uploaded — so the user can
// see precisely what the scrubber removes. The user can then send it on demand and optionally enable auto-reporting for
// that category. Opened from the exception toast. See #1674.
internal class ExceptionReportPageModel : PageModel
{
    private readonly IExceptionReportManager _exceptionReporter;
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;
    private readonly IConfigurationManager _configurationManager;

    public ExceptionReportPageModel(
        IExceptionReportManager exceptionReporter,
        ITelemetryConfigurationService telemetryConfigurationService,
        IConfigurationManager configurationManager )
    {
        this._exceptionReporter = exceptionReporter;
        this._telemetryConfigurationService = telemetryConfigurationService;
        this._configurationManager = configurationManager;
    }

    /// <summary>
    /// Gets or sets the bare file name of the report being reviewed.
    /// </summary>
    [BindProperty]
    public string? Report { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether all reports of this category should be sent automatically from now on.
    /// </summary>
    [BindProperty]
    public bool AutoReport { get; set; }

    /// <summary>
    /// Gets the exact, scrubbed payload that would be uploaded.
    /// </summary>
    public string? ScrubbedContent { get; private set; }

    /// <summary>
    /// Gets the full, unscrubbed local rendering of the same report, or <c>null</c> if it is not available.
    /// </summary>
    public string? LocalContent { get; private set; }

    public bool HasReport => this.ScrubbedContent != null;

    public bool IsReported { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the report has already been sent (auto-reported, or sent on a previous review).
    /// The page then shows the report renderings for transparency but without offering the Report button again. See #1674.
    /// </summary>
    public bool IsAlreadySent { get; private set; }

    // The category is read from the report itself (it is self-contained), never passed as a parameter. See #1674.
    public TelemetryScenario Scenario { get; private set; } = TelemetryScenario.Exception;

    public string CategoryDisplayName => this.Scenario == TelemetryScenario.Performance ? "performance warnings" : "exceptions";

    public void OnGet( string? report )
    {
        this.Report = report;

        if ( !string.IsNullOrEmpty( report ) && this._exceptionReporter.TryGetReport( report!, out var capturedReport ) )
        {
            this.ScrubbedContent = capturedReport.ScrubbedContent;
            this.LocalContent = capturedReport.LocalContent;
            this.Scenario = capturedReport.Category;
            this.IsAlreadySent = capturedReport.IsQueued;
            this.AutoReport = this._configurationManager.Get<TelemetryConfiguration>().GetReportingAction( this.Scenario ) == ReportingAction.Yes;
        }
    }

    public IActionResult OnPostReport()
    {
        if ( !string.IsNullOrEmpty( this.Report ) && this._exceptionReporter.TryGetReport( this.Report!, out var capturedReport ) )
        {
            this.Scenario = capturedReport.Category;
            this.ScrubbedContent = capturedReport.ScrubbedContent;
            this.LocalContent = capturedReport.LocalContent;

            if ( this.AutoReport )
            {
                // Ticking "automatically report all …" enables this category's auto-send going forward, independently of
                // usage telemetry. See #1674.
                this._telemetryConfigurationService.SetStatus( this.Scenario, enabled: true );
            }

            // Only report the report as queued if it was actually enqueued: SendReport can fail (e.g. the file was
            // removed). Keeping the report content set means the page still renders the report rather than a blank form.
            this.IsReported = this._exceptionReporter.SendReport( this.Report! );
        }

        return this.Page();
    }

    // The report has already been auto-sent; this handler only updates whether the category keeps being reported
    // automatically (the checkbox stays available so the user can turn auto-reporting off). It does not re-send. See #1674.
    public IActionResult OnPostUpdateAutoReport()
    {
        if ( !string.IsNullOrEmpty( this.Report ) && this._exceptionReporter.TryGetReport( this.Report!, out var capturedReport ) )
        {
            this.ScrubbedContent = capturedReport.ScrubbedContent;
            this.LocalContent = capturedReport.LocalContent;
            this.Scenario = capturedReport.Category;
            this.IsAlreadySent = capturedReport.IsQueued;

            this._telemetryConfigurationService.SetStatus( this.Scenario, enabled: this.AutoReport );
        }

        return this.Page();
    }
}
