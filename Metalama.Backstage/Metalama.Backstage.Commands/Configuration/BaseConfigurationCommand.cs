// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Linq;

namespace Metalama.Backstage.Commands.Configuration;

internal abstract class BaseConfigurationCommand : BaseCommand<ConfigurationCommandSettings>
{
    protected sealed override void Execute( ExtendedCommandContext context, ConfigurationCommandSettings settings )
    {
        if ( !context.BackstageCommandOptions.ConfigurationFileCommandAdapters.TryGetValue( settings.Alias, out var adapter ) )
        {
            throw new CommandException(
                $"Invalid configuration alias: '{settings.Alias}'. The following configurations are available: {string.Join( ", ", context.BackstageCommandOptions.ConfigurationFileCommandAdapters.Keys.OrderBy( k => k ) )}" );
        }

        this.Execute( context, adapter );
    }

    protected abstract void Execute( ExtendedCommandContext context, ConfigurationFileCommandAdapter adapter );
}