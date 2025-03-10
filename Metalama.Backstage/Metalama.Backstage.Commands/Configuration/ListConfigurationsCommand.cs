// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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