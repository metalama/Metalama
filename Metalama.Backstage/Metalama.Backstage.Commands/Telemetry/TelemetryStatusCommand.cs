// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
        var configurationManager = context.ServiceProvider.GetRequiredBackstageService<IConfigurationManager>();
        var configuration = configurationManager.Get<TelemetryConfiguration>();

        var table = new Table();
        table.AddColumn( "Setting" );
        table.AddColumn( "Value" );

        table.AddRow( "Device ID", configuration.DeviceId?.ToString() ?? "unset" );
        table.AddRow( "Reporting Usage", configuration.UsageReportingAction.ToString() );
        table.AddRow( "Reporting Exceptions", configuration.ExceptionReportingAction.ToString() );
        table.AddRow( "Reporting Performance Problems", configuration.PerformanceProblemReportingAction.ToString() );
        table.AddRow( "Last Uploaded", configuration.LastUploadTime?.ToString( CultureInfo.InvariantCulture ) ?? "(Never)" );

        context.Console.Out.Write( table );
    }
}