// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Spectre.Console;
using System.Globalization;

namespace Metalama.Backstage.Commands.Telemetry;

internal class TelemetryStatusCommand : BaseCommand<BaseCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        var configuration = context.ServiceProvider.GetRequiredBackstageService<IConfigurationManager>().Get<TelemetryConfiguration>();
        var telemetryService = context.ServiceProvider.GetRequiredBackstageService<ITelemetryService>();

        // The current directory is blocked only by the repository-scoped metalama.json opt-out (not by the process-level
        // gates), which is exactly what the tooling policy reports through IsRepositoryOptedOut. See #1701.
        var policy = telemetryService.GetToolingPolicy();

        if ( !policy.HasRepositoryContext )
        {
            context.Console.WriteWarning( "The working directory is not a git repository. Effective values pertain to a specific repository context." );
        }

        var usageConsent = policy.GetConsentAndReason( TelemetryScenario.Usage );
        var performanceConsent = policy.GetConsentAndReason( TelemetryScenario.Performance );
        var exceptionConsent = policy.GetConsentAndReason( TelemetryScenario.Exception );

        var table = new Table();
        table.AddColumn( "Setting" );
        table.AddColumn( "Configured Value" );
        table.AddColumn( "Effective Value" );
        table.AddColumn( "Reason" );

        table.AddRow( "Device ID", configuration.DeviceId?.ToString() ?? "unset" );

        table.AddRow(
            "Usage",
            FormatConsent( TelemetryScenario.Usage, configuration.GetConsent( TelemetryScenario.Usage ) ),
            FormatConsent( TelemetryScenario.Usage, usageConsent.Consent ),
            usageConsent.Reason.ToString() );

        table.AddRow(
            "Exception",
            FormatConsent( TelemetryScenario.Exception, configuration.GetConsent( TelemetryScenario.Exception ) ),
            FormatConsent( TelemetryScenario.Exception, exceptionConsent.Consent ),
            exceptionConsent.Reason.ToString() );

        table.AddRow(
            "Performance",
            FormatConsent( TelemetryScenario.Performance, configuration.GetConsent( TelemetryScenario.Performance ) ),
            FormatConsent( TelemetryScenario.Performance, performanceConsent.Consent ),
            performanceConsent.Reason.ToString() );

        table.AddRow( "Last uploaded", configuration.LastUploadTime?.ToString( CultureInfo.InvariantCulture ) ?? "(Never)" );
        table.AddRow( "Last device id rotation", configuration.LastSaltChangeTime?.ToString( CultureInfo.InvariantCulture ) ?? "(Never)" );

        context.Console.Out.Write( table );
    }

    // Maps a consent to a display string. For the exception and performance scenarios, the review-first 'Default' is
    // shown as "Ask" because that is what it means to the user: the report is captured and they are prompted before it
    // is sent. See #1674, #1707.
    private static string FormatConsent( TelemetryScenario scenario, TelemetryConsent consent )
        => (scenario, consent) switch
        {
            (TelemetryScenario.Exception or TelemetryScenario.Performance, TelemetryConsent.Default) => "Ask",
            (_, TelemetryConsent.Yes) => "Yes",
            (_, TelemetryConsent.No) => "No",
            _ => consent.ToString()
        };
}