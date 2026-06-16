// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Telemetry;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel;
using IConfigurationManager = Metalama.Backstage.Configuration.IConfigurationManager;

namespace Metalama.Backstage.Pages;

#pragma warning disable SA1649

internal class PrivacyPageModel : PageModel
{
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;
    private readonly IConfigurationManager _configurationManager;

    public PrivacyPageModel( ITelemetryConfigurationService telemetryConfigurationService, IConfigurationManager configurationManager )
    {
        this._telemetryConfigurationService = telemetryConfigurationService;
        this._configurationManager = configurationManager;
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
        // configured independently, and a scenario is only changed when its checkbox actually differs from the stored
        // preference — so saving without touching a category preserves a review-first ('Default') category instead of
        // flipping it to auto-send ('Yes'). See #1674.
        this.UpdateIfChanged( TelemetryScenario.Usage, this.IsUsageReportingEnabled );
        this.UpdateIfChanged( TelemetryScenario.Exception, this.IsExceptionReportingEnabled );
        this.UpdateIfChanged( TelemetryScenario.Performance, this.IsPerformanceReportingEnabled );

        this.ReadStatus();
        this.IsSaved = true;

        return this.Page();
    }

    private void UpdateIfChanged( TelemetryScenario scenario, bool enabled )
    {
        if ( this.GetIsEnabled( scenario ) != enabled )
        {
            this._telemetryConfigurationService.SetStatus( scenario, enabled );
        }
    }

    // Reads the stored per-scenario preference directly from the configuration. We intentionally do NOT use
    // ITelemetryConfigurationService.IsEnabled here: the worker is an unattended (and, on a development build,
    // telemetry-disabled) process, so its process-level enablement is always false and IsEnabled would both require
    // consumer-style initialization (device id / salts, usage tracking) and report every category as off regardless of
    // the user's saved choice. This page edits the shared preference, not whether the worker itself sends telemetry.
    private void ReadStatus()
    {
        this.IsUsageReportingEnabled = this.GetIsEnabled( TelemetryScenario.Usage );
        this.IsExceptionReportingEnabled = this.GetIsEnabled( TelemetryScenario.Exception );
        this.IsPerformanceReportingEnabled = this.GetIsEnabled( TelemetryScenario.Performance );
    }

    private bool GetIsEnabled( TelemetryScenario scenario )
    {
        var action = this._configurationManager.Get<TelemetryConfiguration>().GetReportingAction( scenario );

        // Usage reporting is opt-out, so it is "on" unless explicitly disabled. Exception and performance-problem
        // reporting are opt-in to auto-send: the "Send … reports" checkbox reflects auto-send (Yes), so the review-first
        // default (ReportingAction.Default) correctly shows as unchecked. See #1674.
        return scenario == TelemetryScenario.Usage
            ? action != ReportingAction.No
            : action == ReportingAction.Yes;
    }
}
