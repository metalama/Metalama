// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using System;

namespace Metalama.Backstage.Commands.Telemetry;

internal abstract class SetTelemetryCommand : BaseCommand<SetTelemetryCommandSettings>
{
    private readonly TelemetryConsent _consent;

    protected SetTelemetryCommand( TelemetryConsent consent )
    {
        this._consent = consent;
    }

    protected override void Execute( ExtendedCommandContext context, SetTelemetryCommandSettings settings )
    {
        var service = context.ServiceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();

        switch ( settings.Scenario )
        {
            case TelemetryScenarioArgument.Usage:
                service.SetConsent( TelemetryScenario.Usage, this._consent );

                break;

            case TelemetryScenarioArgument.Exception:
                service.SetConsent( TelemetryScenario.Exception, this._consent );

                break;

            case TelemetryScenarioArgument.Performance:
                service.SetConsent( TelemetryScenario.Performance, this._consent );

                break;

            case TelemetryScenarioArgument.All:
                service.SetConsent( this._consent );

                break;

            default:
                throw new ArgumentOutOfRangeException( nameof(settings), settings.Scenario, "Unknown telemetry scenario." );
        }

        var state = this._consent switch
        {
            TelemetryConsent.Yes => "enabled",
            TelemetryConsent.No => "disabled",
            TelemetryConsent.Default => "reset to its default state",
            _ => throw new ArgumentOutOfRangeException( nameof(this._consent), this._consent, "Unknown telemetry consent." )
        };

        var scope = settings.Scenario switch
        {
            TelemetryScenarioArgument.Usage => "Usage telemetry",
            TelemetryScenarioArgument.Exception => "Exception telemetry",
            TelemetryScenarioArgument.Performance => "Performance telemetry",
            _ => "Telemetry"
        };

        context.Console.WriteSuccess( $"{scope} has been {state}." );
    }
}