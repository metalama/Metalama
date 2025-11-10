// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Diagnostics;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Infrastructure;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.UserInterface.Toasts;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Metalama.Backstage.UserInterface.Rss;

internal sealed class RssClient : IRssClient
{
    public const string BriefsUrl = "https://metalama.net/briefs.xml";
    public const string PostsUrl = "https://metalama.net/feed.xml";

    private readonly IConfigurationManager _configurationManager;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ITelemetryConfigurationService _telemetryConfigurationService;
    private readonly IToastNotificationService _toastNotificationService;
    private readonly BackstageBackgroundTasksService _backgroundTasksService;

    public RssClient( IServiceProvider serviceProvider )
    {
        this._configurationManager = serviceProvider.GetRequiredBackstageService<IConfigurationManager>();
        this._dateTimeProvider = serviceProvider.GetRequiredBackstageService<IDateTimeProvider>();
        this._logger = serviceProvider.GetLoggerFactory().GetLogger( nameof(RssClient) );
        this._httpClientFactory = serviceProvider.GetRequiredBackstageService<IHttpClientFactory>();
        this._telemetryConfigurationService = serviceProvider.GetRequiredBackstageService<ITelemetryConfigurationService>();
        this._toastNotificationService = serviceProvider.GetRequiredBackstageService<IToastNotificationService>();
        this._backgroundTasksService = serviceProvider.GetRequiredBackstageService<BackstageBackgroundTasksService>();
    }

    public void Initialize()
    {
        this._backgroundTasksService.Enqueue( () => this.DisplayUnreadNewsAsync() );
    }

    public Task DisplayLatestNewsAsync()
    {
        return this.DisplayUnreadNewsAsync( true );
    }

    public void Disable() => this._configurationManager.Update<RssClientConfiguration>( c => c with { PreferredFeed = RssFeed.None } );

    public void Enable() => this._configurationManager.Update<RssClientConfiguration>( c => c with { PreferredFeed = RssFeed.Briefs } );

    internal async Task DisplayUnreadNewsAsync( bool skipPreconditions = false )
    {
        var configuration = this._configurationManager.Get<RssClientConfiguration>();

        if ( !skipPreconditions )
        {
            // Check preconditions.
            if ( configuration.PreferredFeed == RssFeed.None )
            {
                this._logger.Trace?.Log( "The RSS client has been disabled." );

                return;
            }

            if ( !this._telemetryConfigurationService.IsEnabled( TelemetryScenario.Rss ) )
            {
                this._logger.Trace?.Log( "Telemetry is disabled. Do not fetch news." );

                return;
            }

            if ( configuration.LastFetchTime == null )
            {
                this._logger.Trace?.Log( "This is the first time RssClient fetches news. Do not report any past news." );

                // Never display past items upon first fetch.
                this._configurationManager.Update<RssClientConfiguration>(
                    c => c with { LastFetchTime = this._dateTimeProvider.UtcNow, PreferredFeed = c.PreferredFeed ?? RssFeed.Briefs } );

                return;
            }
            else if ( configuration.LastFetchTime.Value.AddDays( 1 ) > this._dateTimeProvider.UtcNow )
            {
                this._logger.Trace?.Log( $"News were already checked on {configuration.LastFetchTime}." );

                return;
            }
        }

        // Select feed URL.
        var url = configuration.PreferredFeed switch
        {
            null or RssFeed.Briefs => BriefsUrl,
            RssFeed.Posts => PostsUrl,
            _ => null
        };

        if ( url == null )
        {
            this._logger.Warning?.Log( $"Invalid preferred feed: {configuration.PreferredFeed}. Skipping." );

            return;
        }

        try
        {
            // Fetch content.
            var httpClient = this._httpClientFactory.Create();
            var response = await httpClient.GetAsync( url );

            if ( !response.IsSuccessStatusCode )
            {
                this._logger.Trace?.Log( $"Cannot get '{url}': {response.ReasonPhrase}." );

                return;
            }

            var content = await response.Content.ReadAsStringAsync();

            // Try to parse the item.
            if ( !this.TryParseContent( content, out var title, out var link, out var pubDate ) )
            {
                return;
            }

            // Only notify if the item was published after the last fetch time.
            if ( configuration.LastFetchTime != null && pubDate != null && pubDate.Value <= configuration.LastFetchTime!.Value )
            {
                this._logger.Trace?.Log(
                    $"Item published on {pubDate} is not newer than last fetch time {configuration.LastFetchTime}. Skipping notification." );

                return;
            }

            // Create and show a toast notification.
            var notification = new ToastNotification( ToastNotificationKinds.News, title, null, link );
            this._toastNotificationService.Show( notification );
        }
        catch ( Exception e )
        {
            this._logger.LogException( e, "Failed to fetch or parse RSS feed" );
        }
        finally
        {
            // Do not try more than once per day -- both in case of success or failure.
            this._configurationManager.Update<RssClientConfiguration>( c => c with { LastFetchTime = this._dateTimeProvider.UtcNow } );
        }
    }

    private bool TryParseContent(
        string content,
        out string? title,
        out string? url,
        out DateTime? pubDate )
    {
        // Read as XML with restrictive settings security.
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersFromEntities = 1024 };

        using var stringReader = new StringReader( content );
        using var xmlReader = XmlReader.Create( stringReader, settings );

        var xml = XDocument.Load( xmlReader );

        // Parse RSS feed and get the most recent item.
        var channel = xml.Root?.Element( "channel" );

        if ( channel == null )
        {
            this._logger.Warning?.Log( "RSS feed does not contain a channel element." );

            title = null;
            url = null;
            pubDate = null;

            return false;
        }

        var items = channel.Elements( "item" ).ToList();

        if ( items.Count == 0 )
        {
            this._logger.Trace?.Log( "No items found in RSS feed." );

            title = null;
            url = null;
            pubDate = null;

            return false;
        }

        // Get the most recent item (first item in the feed).
        var item = items[0];

        // Extract title.
        title = item.Element( "title" )?.Value;

        if ( string.IsNullOrEmpty( title ) )
        {
            this._logger.Warning?.Log( "RSS item does not contain a title." );
            url = null;
            pubDate = null;

            return false;
        }

        // Extract link.
        url = item.Element( "link" )?.Value;

        if ( string.IsNullOrEmpty( url ) )
        {
            this._logger.Warning?.Log( "RSS item does not contain a link." );
            pubDate = null;

            return false;
        }

        // Append tracking query string parameters using UriBuilder for robustness.
        var uriBuilder = new UriBuilder( url );

        if ( string.IsNullOrEmpty( uriBuilder.Query ) )
        {
            uriBuilder.Query = WebLinks.TrackingQueryString;
        }
        else
        {
            // Remove leading '?' from Query property before appending.
            uriBuilder.Query = uriBuilder.Query.TrimStart( '?' ) + "&" + WebLinks.TrackingQueryString;
        }

        url = uriBuilder.Uri.ToString();

        // Extract pubDate (optional).
        var pubDateString = item.Element( "pubDate" )?.Value;

        if ( !string.IsNullOrEmpty( pubDateString ) )
        {
            if ( DateTime.TryParse( pubDateString, out var parsedDate ) )
            {
                pubDate = parsedDate.ToUniversalTime();
            }
            else
            {
                this._logger.Warning?.Log( $"Failed to parse pubDate: {pubDateString}" );
                pubDate = null;
            }
        }
        else
        {
            this._logger.Trace?.Log( "RSS item does not contain a pubDate element." );
            pubDate = null;
        }

        return true;
    }
}