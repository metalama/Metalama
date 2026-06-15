// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using System;

namespace Metalama.Backstage.Commands.Telemetry;

internal abstract class SetTelemetryCommand : BaseCommand<SetTelemetryCommandSettings>
{
    private readonly bool _enable;

    protected SetTelemetryCommand( bool enable )
    {
        this._enable = enable;
    }

    protected override void Execute( ExtendedCommandContext context, SetTelemetryCommandSettings settings )
    {
        var service = context.ServiceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();

        switch ( settings.Scenario )
        {
            case TelemetryScenarioArgument.Usage:
                service.SetStatus( TelemetryScenario.Usage, this._enable );

                break;

            case TelemetryScenarioArgument.Exception:
                service.SetStatus( TelemetryScenario.Exception, this._enable );

                break;

            case TelemetryScenarioArgument.Performance:
                service.SetStatus( TelemetryScenario.Performance, this._enable );

                break;

            case TelemetryScenarioArgument.All:
                service.SetStatus( this._enable );

                break;

            default:
                throw new ArgumentOutOfRangeException( nameof(settings), settings.Scenario, "Unknown telemetry scenario." );
        }

        var state = this._enable ? "enabled" : "disabled";

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
