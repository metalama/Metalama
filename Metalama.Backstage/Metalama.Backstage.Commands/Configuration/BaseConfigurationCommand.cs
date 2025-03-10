// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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