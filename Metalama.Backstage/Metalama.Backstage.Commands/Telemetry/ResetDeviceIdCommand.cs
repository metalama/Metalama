// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;

namespace Metalama.Backstage.Commands.Telemetry;

internal class ResetDeviceIdCommand : BaseCommand<BaseCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        context.ServiceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>()
            .ResetDeviceId();

        context.Console.WriteSuccess( "The device id has been reset." );
    }
}