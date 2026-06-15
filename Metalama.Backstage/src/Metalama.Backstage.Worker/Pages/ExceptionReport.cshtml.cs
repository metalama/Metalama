// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Telemetry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
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
    /// Gets or sets the bare file name of the report being reviewed.
    /// </summary>
    [BindProperty]
    public string? Report { get; set; }

    /// <summary>
    /// Gets or sets the report category name (<c>Exception</c> or <c>Performance</c>), which drives the checkbox label
    /// and the setting that the checkbox toggles.
    /// </summary>
    [BindProperty]
    public string? Category { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether all reports of this category should be sent automatically from now on.
    /// </summary>
    [BindProperty]
    public bool AutoReport { get; set; }

    public string? ReportContent { get; private set; }

    public bool IsReported { get; private set; }

    public TelemetryScenario Scenario
        => string.Equals( this.Category, nameof(TelemetryScenario.Performance), StringComparison.OrdinalIgnoreCase )
            ? TelemetryScenario.Performance
            : TelemetryScenario.Exception;

    public string CategoryDisplayName => this.Scenario == TelemetryScenario.Performance ? "performance warnings" : "exceptions";

    public void OnGet( string? report, string? category )
    {
        this.Report = report;
        this.Category = category;
        this.ReportContent = string.IsNullOrEmpty( report ) ? null : this._exceptionReporter.TryGetReportContent( report! );
        this.AutoReport = this._configurationManager.Get<TelemetryConfiguration>().GetReportingAction( this.Scenario ) == ReportingAction.Yes;
    }

    public IActionResult OnPostReport()
    {
        if ( this.AutoReport )
        {
            // Ticking "automatically report all …" enables this category's auto-send going forward, independently of
            // usage telemetry. See #1674.
            this._telemetryConfigurationService.SetReportingAction( this.Scenario, ReportingAction.Yes );
        }

        if ( !string.IsNullOrEmpty( this.Report ) )
        {
            this._exceptionReporter.SendReport( this.Report! );
        }

        this.IsReported = true;

        return this.Page();
    }
}
