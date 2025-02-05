// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;

namespace Metalama.Backstage.Commands.Telemetry
{
    internal class UploadTelemetryCommand : BaseCommand<UploadTelemetryCommandSettings>
    {
        protected override void Execute( ExtendedCommandContext context, UploadTelemetryCommandSettings settings )
        {
            var uploader = context.ServiceProvider.GetRequiredBackstageService<ITelemetryUploader>();

            if ( settings.Async )
            {
                uploader.StartUpload( settings.Force );
            }
            else
            {
                uploader.UploadAsync().GetAwaiter().GetResult();
            }
        }
    }
}