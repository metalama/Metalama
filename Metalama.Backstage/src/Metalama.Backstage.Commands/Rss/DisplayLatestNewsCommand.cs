// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.UserInterface.Rss;

namespace Metalama.Backstage.Commands.Rss;

public class DisplayLatestNewsCommand : BaseCommand<BaseCommandSettings>
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        var rssClient = context.ServiceProvider.GetRequiredBackstageService<IRssClient>();
        rssClient.DisplayLatestNewsAsync().Wait();
    }

    protected override BackstageInitializationOptions AddBackstageOptions( BackstageInitializationOptions options )
        => options with { AddUserInterface = true, AddRssClient = true };
}