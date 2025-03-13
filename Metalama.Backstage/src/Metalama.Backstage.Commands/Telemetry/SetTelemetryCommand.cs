// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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