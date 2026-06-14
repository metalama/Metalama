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
    public bool IsTelemetryEnabled { get; set; }

    public bool IsSaved { get; private set; }

    public void OnGet()
    {
        this.IsTelemetryEnabled = this._telemetryConfigurationService.IsEnabled( TelemetryScenario.Usage );
    }

    public IActionResult OnPost()
    {
        this._telemetryConfigurationService.SetStatus( this.IsTelemetryEnabled );

        // Reflect the effective state (it may differ from the request, e.g. when the opt-out environment variable is set).
        this.IsTelemetryEnabled = this._telemetryConfigurationService.IsEnabled( TelemetryScenario.Usage );
        this.IsSaved = true;

        return this.Page();
    }
}
