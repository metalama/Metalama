// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Telemetry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel;

namespace Metalama.Backstage.Pages;

#pragma warning disable SA1649

internal class PrivacyPageModel : PageModel
{
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;

    public PrivacyPageModel( ITelemetryConfigurationService telemetryConfigurationService )
    {
        this._telemetryConfigurationService = telemetryConfigurationService;
    }

    [BindProperty]
    [DisplayName( "Send anonymous usage data" )]
    public bool IsUsageReportingEnabled { get; set; }

    [BindProperty]
    [DisplayName( "Send exception reports" )]
    public bool IsExceptionReportingEnabled { get; set; }

    [BindProperty]
    [DisplayName( "Send performance reports" )]
    public bool IsPerformanceReportingEnabled { get; set; }

    public bool IsSaved { get; private set; }

    public void OnGet()
    {
        this.ReadStatus();
    }

    public IActionResult OnPost()
    {
        // Usage reporting is opt-out; exception and performance-problem reporting are opt-in. Each scenario is
        // configured independently.
        this._telemetryConfigurationService.SetStatus( TelemetryScenario.Usage, this.IsUsageReportingEnabled );
        this._telemetryConfigurationService.SetStatus( TelemetryScenario.Exception, this.IsExceptionReportingEnabled );
        this._telemetryConfigurationService.SetStatus( TelemetryScenario.Performance, this.IsPerformanceReportingEnabled );

        // Reflect the effective state (it may differ from the request, e.g. when the opt-out environment variable is set).
        this.ReadStatus();
        this.IsSaved = true;

        return this.Page();
    }

    private void ReadStatus()
    {
        this.IsUsageReportingEnabled = this._telemetryConfigurationService.IsEnabled( TelemetryScenario.Usage );
        this.IsExceptionReportingEnabled = this._telemetryConfigurationService.IsEnabled( TelemetryScenario.Exception );
        this.IsPerformanceReportingEnabled = this._telemetryConfigurationService.IsEnabled( TelemetryScenario.Performance );
    }
}
