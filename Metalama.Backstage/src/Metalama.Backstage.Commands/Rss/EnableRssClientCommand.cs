// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.UserInterface.Rss;

namespace Metalama.Backstage.Commands.Rss;

public class EnableRssClientCommand : RssCommand
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        var rssClient = context.ServiceProvider.GetRequiredBackstageService<IRssClient>();

        if ( !rssClient.TryEnable() )
        {
            throw new CommandException(
                "Cannot enable the news feed because telemetry is disabled. Enable telemetry first by running 'metalama telemetry enable'." );
        }

        context.Console.WriteSuccess( "The news feed has been enabled." );
    }
}