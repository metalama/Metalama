// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Spectre.Console;
using System.Linq;

namespace Metalama.Backstage.Commands.Configuration;

internal class ListConfigurationsCommand : BaseCommand<BaseCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        var table = new Table();
        table.AddColumn( "Alias" );
        table.AddColumn( "Environment variable" );
        table.AddColumn( "Description" );

        foreach ( var item in context.BackstageCommandOptions.ConfigurationFileCommandAdapters.OrderBy( item => item.Key ) )
        {
            table.AddRow( item.Value.Alias, item.Value.EnvironmentVariableName ?? "(None)", item.Value.Description ?? "" );
        }

        context.Console.Out.Write( table );
    }
}