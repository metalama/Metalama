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

// Lets the user review the exact scrubbed exception/performance report before it is sent, then send it on demand and
// optionally enable auto-reporting for that category. Opened from the exception toast. See #1674.
internal class ExceptionReportPageModel : PageModel
{
    private readonly IExceptionReporter _exceptionReporter;
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;
    private readonly IConfigurationManager _configurationManager;

    public ExceptionReportPageModel(
        IExceptionReporter exceptionReporter,
        ITelemetryConfigurationService telemetryConfigurationService,
        IConfigurationManager configurationManager )
    {
        this._exceptionReporter = exceptionReporter;
        this._telemetryConfigurationService = telemetryConfigurationService;
        this._configurationManager = configurationManager;
    }

    /// <summary>
    /// Gets or sets the bare file name (the report id) of the report being reviewed.
    /// </summary>
    [BindProperty]
    public string? Report { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether all reports of this category should be sent automatically from now on.
    /// </summary>
    [BindProperty]
    public bool AutoReport { get; set; }

    public string? ReportContent { get; private set; }

    public bool IsReported { get; private set; }

    /// <summary>
    /// Gets the report category. It is read from the report itself (which is self-contained), not passed as a parameter.
    /// Defaults to <see cref="TelemetryScenario.Exception"/> until a report has been loaded. See #1674.
    /// </summary>
    public TelemetryScenario Scenario { get; private set; } = TelemetryScenario.Exception;

    public string CategoryDisplayName => this.Scenario == TelemetryScenario.Performance ? "performance warnings" : "exceptions";

    public void OnGet( string? report )
    {
        this.Report = report;

        if ( !string.IsNullOrEmpty( report ) && this._exceptionReporter.TryGetReport( report!, out var localReport ) )
        {
            this.ReportContent = localReport.Content;
            this.Scenario = localReport.Scenario;
            this.AutoReport = this._configurationManager.Get<TelemetryConfiguration>().GetReportingAction( this.Scenario ) == ReportingAction.Yes;
        }
    }

    public IActionResult OnPostReport()
    {
        // Re-read the report so the category (and thus which setting the checkbox toggles) comes from the report itself,
        // keeping the report self-contained rather than trusting a round-tripped form value. See #1674.
        if ( string.IsNullOrEmpty( this.Report ) || !this._exceptionReporter.TryGetReport( this.Report!, out var localReport ) )
        {
            // The report is gone (e.g. already sent or removed); there is nothing left to report.
            return this.Page();
        }

        this.Scenario = localReport.Scenario;
        this.ReportContent = localReport.Content;

        if ( this.AutoReport )
        {
            // Ticking "automatically report all …" enables this category's auto-send going forward, independently of
            // usage telemetry. See #1674.
            this._telemetryConfigurationService.SetStatus( this.Scenario, enabled: true );
        }

        this.IsReported = this._exceptionReporter.SendReport( this.Report! );

        return this.Page();
    }
}
