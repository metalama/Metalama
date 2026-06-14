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
    [DisplayName( "Send exception and performance reports" )]
    public bool IsExceptionReportingEnabled { get; set; }

    public bool IsSaved { get; private set; }

    public void OnGet()
    {
        this.ReadStatus();
    }

    public IActionResult OnPost()
    {
        // Usage reporting is opt-out; exception and performance-problem reporting are opt-in. They are configured
        // independently. Performance-problem reports are sent through the same channel as exception reports, so they
        // follow the same toggle.
        this._telemetryConfigurationService.SetStatus( TelemetryScenario.Usage, this.IsUsageReportingEnabled );
        this._telemetryConfigurationService.SetStatus( TelemetryScenario.Exception, this.IsExceptionReportingEnabled );
        this._telemetryConfigurationService.SetStatus( TelemetryScenario.Performance, this.IsExceptionReportingEnabled );

        // Reflect the effective state (it may differ from the request, e.g. when the opt-out environment variable is set).
        this.ReadStatus();
        this.IsSaved = true;

        return this.Page();
    }

    private void ReadStatus()
    {
        this.IsUsageReportingEnabled = this._telemetryConfigurationService.IsEnabled( TelemetryScenario.Usage );
        this.IsExceptionReportingEnabled = this._telemetryConfigurationService.IsEnabled( TelemetryScenario.Exception );
    }
}
