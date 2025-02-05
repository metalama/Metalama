// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;

namespace Metalama.Backstage.Commands.Telemetry;

internal abstract class SetTelemetryCommand : BaseCommand<BaseCommandSettings>
{
    private readonly bool _enable;

    protected SetTelemetryCommand( bool enable )
    {
        this._enable = enable;
    }

    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        context.ServiceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>().SetStatus( this._enable );
        var state = this._enable ? "enabled" : "disabled";
        context.Console.WriteSuccess( $"Telemetry has been {state}." );
    }
}