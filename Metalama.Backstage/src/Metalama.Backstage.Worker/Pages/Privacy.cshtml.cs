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

    // Usage telemetry is a genuine two-state opt-out (Yes/No), so it stays a checkbox.
    [BindProperty]
    [DisplayName( "Send anonymous usage data" )]
    public bool IsUsageReportingEnabled { get; set; }

    // Exception and performance reporting are three-state (see TelemetryConsent): No (never), Default (review-first:
    // capture locally and ask before sending) and Yes (auto-send). They bind to a radio group, not a checkbox. See #1707.
    [BindProperty]
    [DisplayName( "Exception reports" )]
    public TelemetryConsent ExceptionConsent { get; set; }

    [BindProperty]
    [DisplayName( "Performance reports" )]
    public TelemetryConsent PerformanceConsent { get; set; }

    public bool IsSaved { get; private set; }

    // Whether telemetry has been activated, i.e. a device ID exists. The "reset device ID" action is only meaningful (and
    // only offered) in that case: rotating a never-activated configuration would create a device ID and thereby activate
    // telemetry, which must stay lazy. See #1701.
    public bool IsTelemetryActivated { get; private set; }

    public bool IsDeviceIdReset { get; private set; }

    public void OnGet()
    {
        this.ReadStatus();
    }

    public IActionResult OnPostResetDeviceId()
    {
        if ( this._telemetryConfigurationService.IsActivated )
        {
            // Regenerates the device ID and the per-channel salts so past and future reports can no longer be correlated.
            this._telemetryConfigurationService.ResetDeviceId();
            this.IsDeviceIdReset = true;
        }

        this.ReadStatus();

        return this.Page();
    }

    public IActionResult OnPost()
    {
        // Each scenario is configured independently. Usage is opt-out (Yes/No), while exception and performance carry the
        // full three-state consent including the review-first 'Default'. We only write a scenario whose stored consent
        // actually changed, so saving without touching a category never clobbers it. See #1674, #1707.
        this.SetConsentIfChanged( TelemetryScenario.Usage, this.IsUsageReportingEnabled ? TelemetryConsent.Yes : TelemetryConsent.No );
        this.SetConsentIfChanged( TelemetryScenario.Exception, this.ExceptionConsent );
        this.SetConsentIfChanged( TelemetryScenario.Performance, this.PerformanceConsent );

        this.ReadStatus();
        this.IsSaved = true;

        return this.Page();
    }

    private void SetConsentIfChanged( TelemetryScenario scenario, TelemetryConsent consent )
    {
        if ( this.GetConsent( scenario ) != consent )
        {
            this._telemetryConfigurationService.SetConsent( scenario, consent );
        }
    }

    // Reads the stored per-scenario preference directly from the configuration. We intentionally do NOT use
    // ITelemetryConfigurationService.GetEffectiveConsent here: the worker is an unattended (and, on a development build,
    // telemetry-disabled) process, so its process-level enablement is always false and GetEffectiveConsent would both
    // require consumer-style initialization (device id / salts, usage tracking) and report every category as 'No'
    // regardless of the user's saved choice. This page edits the shared preference, not whether the worker itself sends
    // telemetry.
    private void ReadStatus()
    {
        // Usage reporting is opt-out, so it is "on" unless explicitly disabled. Exception and performance reporting
        // expose their full three-state consent so the page can distinguish 'Default' (review-first) from 'No'. See #1707.
        this.IsUsageReportingEnabled = this.GetConsent( TelemetryScenario.Usage ) != TelemetryConsent.No;
        this.ExceptionConsent = this.GetConsent( TelemetryScenario.Exception );
        this.PerformanceConsent = this.GetConsent( TelemetryScenario.Performance );
        this.IsTelemetryActivated = this._telemetryConfigurationService.IsActivated;
    }

    private TelemetryConsent GetConsent( TelemetryScenario scenario ) => this._configurationManager.Get<TelemetryConfiguration>().GetConsent( scenario );
}