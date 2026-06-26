// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.UserInterface.Rss;
using Spectre.Console;
using System.Globalization;
using IConfigurationManager = Metalama.Backstage.Configuration.IConfigurationManager;

namespace Metalama.Backstage.Commands.Rss;

public class RssStatusCommand : RssCommand
{
    protected override void Execute( ExtendedCommandContext context, BaseCommandSettings settings )
    {
        var configuration = context.ServiceProvider.GetRequiredBackstageService<IConfigurationManager>().Get<RssClientConfiguration>();

        // Check if rss is blocked by telemetry settings.
        var client = context.ServiceProvider.GetRequiredBackstageService<IRssClient>();
        var telemetryService = context.ServiceProvider.GetRequiredBackstageService<ITelemetryService>();

        // The current directory is blocked only by the repository-scoped metalama.json opt-out (not by the process-level
        // gates), which is exactly what the tooling policy reports through IsRepositoryOptedOut. See #1701.
        var telemetryPolicy = telemetryService.GetToolingPolicy();
        var telemetryContext = telemetryService.OpenContext( telemetryPolicy );

        var rssDisabledReason = client.GetDisabledReason( telemetryContext );

        if ( rssDisabledReason is not (TelemetryDisabledReason.None or TelemetryDisabledReason.UserOptOut) )
        {
            context.Console.WriteWarning( $"Fetching the RSS feed is not allowed: {rssDisabledReason}." );
        }

        var table = new Table();
        table.AddColumn( "Setting" );
        table.AddColumn( "Value" );

        // The feed is disabled only when explicitly set to None. An unset (null) preferred feed defaults to Briefs, so it
        // is enabled. See RssClient.DisplayNewsAsync.
        table.AddRow( "News notifications", configuration.PreferredFeed == RssFeed.None ? "Disabled" : "Enabled" );
        table.AddRow( "Feed", FormatFeed( configuration.PreferredFeed ) );
        table.AddRow( "Last fetched", configuration.LastFetchTime?.ToString( CultureInfo.InvariantCulture ) ?? "(Never)" );

        context.Console.Out.Write( table );
    }

    private static string FormatFeed( RssFeed? feed )
        => feed switch
        {
            null => "Briefs (default)",
            RssFeed.None => "None",
            _ => feed.ToString()!
        };

 
}