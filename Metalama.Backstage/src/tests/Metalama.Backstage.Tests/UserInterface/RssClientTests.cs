// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Testing;
using Metalama.Backstage.UserInterface;
using Metalama.Backstage.UserInterface.Rss;
using Metalama.Backstage.UserInterface.Toasts;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.UserInterface;

/// <summary>
/// Tests for the RssClient class, which fetches RSS feeds (briefs and posts) from metalama.net
/// and displays toast notifications for new content. Tests cover configuration handling, fetch frequency,
/// feed selection, RSS parsing, HTTP communication, toast notifications, and error handling.
/// </summary>
public sealed class RssClientTests : TestsBase
{
    public RssClientTests( ITestOutputHelper logger ) : base( logger, new TestApplicationInfo() { IsTelemetryEnabled = true } ) { }

    private void UpdateConfiguration( Func<RssClientConfiguration, RssClientConfiguration> func ) => this.ConfigurationManager!.Update( func );

    private void EnsureNewsWillBeChecked() => this.UpdateConfiguration( c => c with { LastFetchTime = DateTime.MinValue } );

    private void RegisterResponse( string url, string response )
        => this.HttpClientFactory.AddHook(
            r => r.RequestUri!.ToString().StartsWith( url, StringComparison.Ordinal ),
            ( _, _ ) => Task.FromResult( new HttpResponseMessage( HttpStatusCode.Accepted ) { Content = new StringContent( response ) } ) );

    // Configuration and Initialization Tests

    /// <summary>
    /// Verifies that RssClient does not fetch RSS feeds when PreferredFeed is set to RssFeed.None.
    /// </summary>
    [Fact]
    public async Task RssClientDoesNotFetchWhenFeedIsDisabled()
    {
        this.EnsureNewsWillBeChecked();
        this.UpdateConfiguration( c => c with { PreferredFeed = RssFeed.None } );

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        Assert.Empty( this.HttpClientFactory.ProcessedRequests );
    }

    /// <summary>
    /// Verifies that RssClient does not fetch RSS feeds when telemetry for RSS scenario is disabled.
    /// </summary>
    [Fact]
    public async Task RssClientDoesNotFetchWhenTelemetryIsDisabled()
    {
        this.EnvironmentVariableProvider.Environment[Backstage.Telemetry.TelemetryConfigurationService.OptOutEnvironmentVariable] = "1";
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        Assert.Empty( this.HttpClientFactory.ProcessedRequests );
    }

    /// <summary>
    /// Verifies that RssClient sets LastFetchTime to current UTC time on first initialization.
    /// </summary>
    [Fact]
    public async Task RssClientSetsLastFetchTimeAndPreferredFeedOnFirstRun()
    {
        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();
        var configuration = this.ConfigurationManager!.Get<RssClientConfiguration>();

        Assert.NotNull( configuration.LastFetchTime );
        Assert.Equal( RssFeed.Briefs, configuration.PreferredFeed );
    }

    /// <summary>
    /// Verifies that RssClient does not display toast notifications for past news items on the first fetch,
    /// only updating LastFetchTime without showing any notifications.
    /// </summary>
    [Fact]
    public async Task RssClientDoesNotShowPastNewsOnFirstFetch()
    {
        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        Assert.Empty( this.HttpClientFactory.ProcessedRequests );
        Assert.Empty( this.UserInterface.Notifications );
    }

    // Fetch Frequency Tests

    /// <summary>
    /// Verifies that RssClient does not attempt to fetch RSS feeds when less than one day has passed since LastFetchTime.
    /// </summary>
    [Fact]
    public async Task RssClientDoesNotFetchWithinOneDayOfLastFetch()
    {
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();
        Assert.Single( this.HttpClientFactory.ProcessedRequests );

        this.Time.AddTime( TimeSpan.FromHours( 1 ) );
        this.HttpClientFactory.ClearProcessedRequests();
        await rssClient.DisplayUnreadNewsAsync();
        Assert.Empty( this.HttpClientFactory.ProcessedRequests );
    }

    /// <summary>
    /// Verifies that RssClient fetches RSS feeds when more than one day has passed since LastFetchTime.
    /// </summary>
    [Fact]
    public async Task RssClientFetchesAfterOneDayFromLastFetch()
    {
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();
        Assert.Single( this.HttpClientFactory.ProcessedRequests );

        this.Time.AddTime( TimeSpan.FromHours( 25 ) );
        this.HttpClientFactory.ClearProcessedRequests();
        await rssClient.DisplayUnreadNewsAsync();
        Assert.Single( this.HttpClientFactory.ProcessedRequests );
    }

    // Feed Selection Tests

    /// <summary>
    /// Verifies that RssClient uses the correct URL based on the PreferredFeed configuration
    /// (https://metalama.net/briefs.xml for Briefs, https://metalama.net/feed.xml for Posts).
    /// </summary>
    [Theory]
    [InlineData( RssFeed.Briefs, RssClient.BriefsUrl )]
    [InlineData( RssFeed.Posts, RssClient.PostsUrl )]
    public async Task RssClientUsesCorrectFeedUrl( RssFeed preferredFeed, string expectedFeed )
    {
        this.UpdateConfiguration( c => c with { PreferredFeed = preferredFeed } );
        this.EnsureNewsWillBeChecked();
        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();
        var request = Assert.Single( this.HttpClientFactory.ProcessedRequests ).Request;

        Assert.Equal( expectedFeed, request.RequestUri!.ToString() );
    }

    /// <summary>
    /// Verifies that RssClient skips fetching when PreferredFeed is set to an invalid value.
    /// </summary>
    [Fact]
    public async Task RssClientSkipsInvalidPreferredFeed()
    {
        this.UpdateConfiguration( c => c with { PreferredFeed = (RssFeed) 100 } );
        this.EnsureNewsWillBeChecked();
        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();
        Assert.Empty( this.HttpClientFactory.ProcessedRequests );
    }

    // RSS Parsing Tests

    /// <summary>
    /// Verifies that RssClient successfully parses a valid RSS feed XML and extracts title and link from the most recent item.
    /// </summary>
    [Fact]
    public async Task RssClientParsesValidRssFeedSuccessfully()
    {
        const string rssXml = $"""
                               <?xml version="1.0" encoding="UTF-8"?>
                               <rss version="2.0">
                                 <channel>
                                   <title>Test Feed</title>
                                   <link>https://metalama.net/</link>
                                   <description>Test Description</description>
                                   <item>
                                     <title>Latest News Article</title>
                                     <link>https://metalama.net/latest-news</link>
                                     <pubDate>Mon, 01 Nov 2025 12:00:00 GMT</pubDate>
                                     <description>This is the latest article</description>
                                   </item>
                                   <item>
                                     <title>Older News Article</title>
                                     <link>https://metalama.net/older-news</link>
                                     <pubDate>Mon, 25 Oct 2025 12:00:00 GMT</pubDate>
                                     <description>This is an older article</description>
                                   </item>
                                 </channel>
                               </rss>
                               """;

        this.RegisterResponse( RssClient.BriefsUrl, rssXml );
        this.Time.Set( new DateTime( 2025, 11, 2, 0, 0, 0, DateTimeKind.Utc ) );
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        var notification = Assert.Single( this.UserInterface.Notifications );
        Assert.Equal( "Latest News Article", notification.Title );
        Assert.Equal( "https://metalama.net/latest-news?" + WebLinks.TrackingQueryString, notification.Uri );
        Assert.Equal( ToastNotificationKinds.News, notification.Kind );
    }

    /// <summary>
    /// Verifies that RssClient logs a warning and returns false when the RSS feed XML does not contain a channel element.
    /// </summary>
    [Fact]
    public async Task RssClientHandlesMissingChannelElement()
    {
        const string rssXml = """
                              <?xml version="1.0" encoding="UTF-8"?>
                              <rss version="2.0">
                                <item>
                                  <title>Test Article</title>
                                  <link>https://metalama.net/test</link>
                                </item>
                              </rss>
                              """;

        this.RegisterResponse( RssClient.BriefsUrl, rssXml );
        this.Time.Set( new DateTime( 2025, 11, 2, 0, 0, 0, DateTimeKind.Utc ) );
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        Assert.Empty( this.UserInterface.Notifications );
    }

    /// <summary>
    /// Verifies that RssClient handles RSS feeds with no items.
    /// </summary>
    [Fact]
    public async Task RssClientHandlesEmptyFeedWithNoItems()
    {
        const string rssXml = """
                              <?xml version="1.0" encoding="UTF-8"?>
                              <rss version="2.0">
                                <channel>
                                  <title>Test Feed</title>
                                  <link>https://metalama.net/</link>
                                  <description>Test Description</description>
                                </channel>
                              </rss>
                              """;

        this.RegisterResponse( RssClient.BriefsUrl, rssXml );
        this.Time.Set( new DateTime( 2025, 11, 2, 0, 0, 0, DateTimeKind.Utc ) );
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        Assert.Empty( this.UserInterface.Notifications );
    }

    /// <summary>
    /// Verifies that RssClient logs a warning and returns false when an RSS item does not contain a title element.
    /// </summary>
    [Fact]
    public async Task RssClientHandlesMissingTitleInRssItem()
    {
        const string rssXml = """
                              <?xml version="1.0" encoding="UTF-8"?>
                              <rss version="2.0">
                                <channel>
                                  <title>Test Feed</title>
                                  <item>
                                    <link>https://metalama.net/test</link>
                                    <pubDate>Mon, 01 Nov 2025 12:00:00 GMT</pubDate>
                                  </item>
                                </channel>
                              </rss>
                              """;

        this.RegisterResponse( RssClient.BriefsUrl, rssXml );
        this.Time.Set( new DateTime( 2025, 11, 2, 0, 0, 0, DateTimeKind.Utc ) );
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        Assert.Empty( this.UserInterface.Notifications );
    }

    /// <summary>
    /// Verifies that RssClient logs a warning and returns false when an RSS item does not contain a link element.
    /// </summary>
    [Fact]
    public async Task RssClientHandlesMissingLinkInRssItem()
    {
        const string rssXml = """
                              <?xml version="1.0" encoding="UTF-8"?>
                              <rss version="2.0">
                                <channel>
                                  <title>Test Feed</title>
                                  <item>
                                    <title>Test Article</title>
                                    <pubDate>Mon, 01 Nov 2025 12:00:00 GMT</pubDate>
                                  </item>
                                </channel>
                              </rss>
                              """;

        this.RegisterResponse( RssClient.BriefsUrl, rssXml );
        this.Time.Set( new DateTime( 2025, 11, 2, 0, 0, 0, DateTimeKind.Utc ) );
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        Assert.Empty( this.UserInterface.Notifications );
    }

    /// <summary>
    /// Verifies that RssClient logs a warning and returns false when an RSS item has an empty title element.
    /// </summary>
    [Fact]
    public async Task RssClientHandlesEmptyTitleInRssItem()
    {
        const string rssXml = """
                              <?xml version="1.0" encoding="UTF-8"?>
                              <rss version="2.0">
                                <channel>
                                  <title>Test Feed</title>
                                  <item>
                                    <title></title>
                                    <link>https://metalama.net/test</link>
                                    <pubDate>Mon, 01 Nov 2025 12:00:00 GMT</pubDate>
                                  </item>
                                </channel>
                              </rss>
                              """;

        this.RegisterResponse( RssClient.BriefsUrl, rssXml );
        this.Time.Set( new DateTime( 2025, 11, 2, 0, 0, 0, DateTimeKind.Utc ) );
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        Assert.Empty( this.UserInterface.Notifications );
    }

    /// <summary>
    /// Verifies that RssClient logs a warning and returns false when an RSS item has an empty link element.
    /// </summary>
    [Fact]
    public async Task RssClientHandlesEmptyLinkInRssItem()
    {
        const string rssXml = """
                              <?xml version="1.0" encoding="UTF-8"?>
                              <rss version="2.0">
                                <channel>
                                  <title>Test Feed</title>
                                  <item>
                                    <title>Test Article</title>
                                    <link></link>
                                    <pubDate>Mon, 01 Nov 2025 12:00:00 GMT</pubDate>
                                  </item>
                                </channel>
                              </rss>
                              """;

        this.RegisterResponse( RssClient.BriefsUrl, rssXml );
        this.Time.Set( new DateTime( 2025, 11, 2, 0, 0, 0, DateTimeKind.Utc ) );
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        Assert.Empty( this.UserInterface.Notifications );
    }

    /// <summary>
    /// Verifies that RssClient handles malformed XML by catching and logging the parsing exception.
    /// </summary>
    [Fact]
    public async Task RssClientHandlesMalformedXml()
    {
        const string rssXml = """
                              <?xml version="1.0" encoding="UTF-8"?>
                              <rss version="2.0">
                                <channel>
                                  <title>Test Feed</title>
                                  <item>
                                    <title>Test Article
                              """;

        this.RegisterResponse( RssClient.BriefsUrl, rssXml );
        this.Time.Set( new DateTime( 2025, 11, 2, 0, 0, 0, DateTimeKind.Utc ) );
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        Assert.Empty( this.UserInterface.Notifications );

        // Verify that LastFetchTime was updated even though parsing failed.
        var configuration = this.ConfigurationManager!.Get<RssClientConfiguration>();
        Assert.NotNull( configuration.LastFetchTime );
    }

    // Error Handling and Recovery Tests

    /// <summary>
    /// Verifies that RssClient handles exceptions gracefully and updates LastFetchTime,
    /// and that the notification is displayed the next day if the error is fixed.
    /// </summary>
    [Fact]
    public async Task RssClientHandlesExceptions()
    {
        // First, set up an exception response
        this.HttpClientFactory.AddHook(
            r => r.RequestUri!.ToString().StartsWith( RssClient.BriefsUrl, StringComparison.Ordinal ),
            ( _, _ ) => throw new HttpRequestException( "Network error" ) );

        this.Time.Set( new DateTime( 2025, 11, 1, 0, 0, 0, DateTimeKind.Utc ) );
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        // Verify no notification was shown
        Assert.Empty( this.UserInterface.Notifications );

        // Verify that LastFetchTime was updated even though an exception occurred
        var configuration = this.ConfigurationManager!.Get<RssClientConfiguration>();
        Assert.NotNull( configuration.LastFetchTime );

        // Now advance time by more than one day and provide a valid response
        this.Time.AddTime( TimeSpan.FromDays( 3 ) );

        const string rssXml = """
                              <?xml version="1.0" encoding="UTF-8"?>
                              <rss version="2.0">
                                <channel>
                                  <title>Test Feed</title>
                                  <item>
                                    <title>Recovered News Article</title>
                                    <link>https://metalama.net/recovered</link>
                                    <pubDate>Mon, 03 Nov 2025 12:00:00 GMT</pubDate>
                                  </item>
                                </channel>
                              </rss>
                              """;

        this.HttpClientFactory.ClearHooks();
        this.HttpClientFactory.ClearProcessedRequests();
        this.RegisterResponse( RssClient.BriefsUrl, rssXml );

        await rssClient.DisplayUnreadNewsAsync();

        // Verify the notification is now displayed
        var notification = Assert.Single( this.UserInterface.Notifications );
        Assert.Equal( "Recovered News Article", notification.Title );
        Assert.Equal( "https://metalama.net/recovered?" + WebLinks.TrackingQueryString, notification.Uri );
    }

    /// <summary>
    /// Verifies that RssClient handles error response codes,
    /// and that the notification is displayed the next day if the error is fixed. 
    /// </summary>
    [Fact]
    public async Task RssClientHttpResponseStatus()
    {
        // First, set up an error response (500 Internal Server Error)
        this.HttpClientFactory.AddHook(
            r => r.RequestUri!.ToString().StartsWith( RssClient.BriefsUrl, StringComparison.Ordinal ),
            ( _, _ ) => Task.FromResult(
                new HttpResponseMessage( HttpStatusCode.InternalServerError ) { Content = new StringContent( "Internal Server Error" ) } ) );

        this.Time.Set( new DateTime( 2025, 11, 1, 0, 0, 0, DateTimeKind.Utc ) );
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        // Verify no notification was shown
        Assert.Empty( this.UserInterface.Notifications );

        // Verify that LastFetchTime was updated even with error response
        var configuration = this.ConfigurationManager!.Get<RssClientConfiguration>();
        Assert.NotNull( configuration.LastFetchTime );

        // Now advance time by more than one day and provide a valid response
        this.Time.AddTime( TimeSpan.FromDays( 2 ) );

        const string rssXml = """
                              <?xml version="1.0" encoding="UTF-8"?>
                              <rss version="2.0">
                                <channel>
                                  <title>Test Feed</title>
                                  <item>
                                    <title>Server Recovered Article</title>
                                    <link>https://metalama.net/server-recovered</link>
                                    <pubDate>Mon, 03 Nov 2025 12:00:00 GMT</pubDate>
                                  </item>
                                </channel>
                              </rss>
                              """;

        this.HttpClientFactory.ClearProcessedRequests();
        this.HttpClientFactory.ClearHooks();
        this.RegisterResponse( RssClient.BriefsUrl, rssXml );

        await rssClient.DisplayUnreadNewsAsync();

        // Verify the notification is now displayed
        var notification = Assert.Single( this.UserInterface.Notifications );
        Assert.Equal( "Server Recovered Article", notification.Title );
        Assert.Equal( "https://metalama.net/server-recovered?" + WebLinks.TrackingQueryString, notification.Uri );
    }

    /// <summary>
    /// Verifies that RssClient updates LastFetchTime to current UTC time after successfully fetching and parsing an RSS feed.
    /// </summary>
    [Fact]
    public async Task RssClientUpdatesLastFetchTimeOnSuccess()
    {
        const string rssXml = """
                              <?xml version="1.0" encoding="UTF-8"?>
                              <rss version="2.0">
                                <channel>
                                  <title>Test Feed</title>
                                  <item>
                                    <title>Test Article</title>
                                    <link>https://metalama.net/test</link>
                                    <pubDate>Mon, 01 Nov 2025 12:00:00 GMT</pubDate>
                                  </item>
                                </channel>
                              </rss>
                              """;

        this.RegisterResponse( RssClient.BriefsUrl, rssXml );

        var expectedTime = new DateTime( 2025, 11, 2, 10, 30, 0, DateTimeKind.Utc );
        this.Time.Set( expectedTime );
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        // Verify LastFetchTime was updated to the current time
        var configuration = this.ConfigurationManager!.Get<RssClientConfiguration>();
        Assert.NotNull( configuration.LastFetchTime );
        Assert.Equal( expectedTime, configuration.LastFetchTime.Value );
    }

    /// <summary>
    /// Verifies that RssClient does not re-display news items that were already shown (published before LastFetchTime),
    /// but only displays new items published after LastFetchTime.
    /// </summary>
    [Fact]
    public async Task RssClientDoesNotRedisplayOldNewsButShowsNewNews()
    {
        // First fetch: Display initial news item
        const string initialRssXml = """
                                     <?xml version="1.0" encoding="UTF-8"?>
                                     <rss version="2.0">
                                       <channel>
                                         <title>Test Feed</title>
                                         <item>
                                           <title>Initial News Article</title>
                                           <link>https://metalama.net/initial-news</link>
                                           <pubDate>Mon, 01 Nov 2025 12:00:00 GMT</pubDate>
                                         </item>
                                       </channel>
                                     </rss>
                                     """;

        this.RegisterResponse( RssClient.BriefsUrl, initialRssXml );
        this.Time.Set( new DateTime( 2025, 11, 1, 0, 0, 0, DateTimeKind.Utc ) );
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        // Verify first notification was shown
        var firstNotification = Assert.Single( this.UserInterface.Notifications );
        Assert.Equal( "Initial News Article", firstNotification.Title );

        // Clear notifications and advance time
        this.UserInterface.Notifications.Clear();
        this.Time.AddTime( TimeSpan.FromDays( 2 ) );

        // Second fetch: RSS feed now has the old item plus a new item
        const string updatedRssXml = """
                                     <?xml version="1.0" encoding="UTF-8"?>
                                     <rss version="2.0">
                                       <channel>
                                         <title>Test Feed</title>
                                         <item>
                                           <title>New News Article</title>
                                           <link>https://metalama.net/new-news</link>
                                           <pubDate>Thu, 03 Nov 2025 12:00:00 GMT</pubDate>
                                         </item>
                                         <item>
                                           <title>Initial News Article</title>
                                           <link>https://metalama.net/initial-news</link>
                                           <pubDate>Mon, 01 Nov 2025 12:00:00 GMT</pubDate>
                                         </item>
                                       </channel>
                                     </rss>
                                     """;

        this.HttpClientFactory.ClearHooks();
        this.RegisterResponse( RssClient.BriefsUrl, updatedRssXml );
        this.HttpClientFactory.ClearProcessedRequests();

        await rssClient.DisplayUnreadNewsAsync();

        // Verify only the new notification is shown, not the old one
        var secondNotification = Assert.Single( this.UserInterface.Notifications );
        Assert.Equal( "New News Article", secondNotification.Title );
        Assert.Equal( "https://metalama.net/new-news?" + WebLinks.TrackingQueryString, secondNotification.Uri );
    }

    /// <summary>
    /// Verifies that RssClient correctly appends tracking query string parameters to URLs that already contain query parameters.
    /// </summary>
    [Fact]
    public async Task RssClientAppendsTrackingParametersToUrlWithExistingQueryString()
    {
        const string rssXml = """
                              <?xml version="1.0" encoding="UTF-8"?>
                              <rss version="2.0">
                                <channel>
                                  <title>Test Feed</title>
                                  <item>
                                    <title>Article With Query Params</title>
                                    <link>https://metalama.net/article?id=123&amp;source=feed</link>
                                    <pubDate>Mon, 01 Nov 2025 12:00:00 GMT</pubDate>
                                  </item>
                                </channel>
                              </rss>
                              """;

        this.RegisterResponse( RssClient.BriefsUrl, rssXml );
        this.Time.Set( new DateTime( 2025, 11, 2, 0, 0, 0, DateTimeKind.Utc ) );
        this.EnsureNewsWillBeChecked();

        var rssClient = new RssClient( this.ServiceProvider );
        await rssClient.DisplayUnreadNewsAsync();

        var notification = Assert.Single( this.UserInterface.Notifications );
        Assert.Equal( "Article With Query Params", notification.Title );
        Assert.Equal( "https://metalama.net/article?id=123&source=feed&" + WebLinks.TrackingQueryString, notification.Uri );
        Assert.Equal( ToastNotificationKinds.News, notification.Kind );
    }

    // Enable/Disable Tests

    /// <summary>
    /// Verifies that RssClient.Enable sets PreferredFeed to Briefs and RssClient.Disable sets PreferredFeed to None.
    /// </summary>
    [Fact]
    public async Task RssClientEnableAndDisableTogglePreferredFeed()
    {
        var rssClient = new RssClient( this.ServiceProvider );

        // Enable RSS client
        rssClient.Enable();
        var enabledConfiguration = this.ConfigurationManager!.Get<RssClientConfiguration>();
        Assert.Equal( RssFeed.Briefs, enabledConfiguration.PreferredFeed );

        // Disable RSS client
        rssClient.Disable();
        var disabledConfiguration = this.ConfigurationManager!.Get<RssClientConfiguration>();
        Assert.Equal( RssFeed.None, disabledConfiguration.PreferredFeed );

        // Verify that disabled RSS client does not fetch news
        this.EnsureNewsWillBeChecked();
        await rssClient.DisplayUnreadNewsAsync();
        Assert.Empty( this.HttpClientFactory.ProcessedRequests );

        // Enable again and verify it can fetch
        rssClient.Enable();

        const string rssXml = """
                              <?xml version="1.0" encoding="UTF-8"?>
                              <rss version="2.0">
                                <channel>
                                  <title>Test Feed</title>
                                  <item>
                                    <title>Test Article After Enable</title>
                                    <link>https://metalama.net/test-after-enable</link>
                                    <pubDate>Mon, 01 Nov 2025 12:00:00 GMT</pubDate>
                                  </item>
                                </channel>
                              </rss>
                              """;

        this.RegisterResponse( RssClient.BriefsUrl, rssXml );
        this.Time.Set( new DateTime( 2025, 11, 2, 0, 0, 0, DateTimeKind.Utc ) );

        await rssClient.DisplayUnreadNewsAsync();
        Assert.Single( this.HttpClientFactory.ProcessedRequests );
        var notification = Assert.Single( this.UserInterface.Notifications );
        Assert.Equal( "Test Article After Enable", notification.Title );
    }

    // DisplayLatestNewsAsync Tests

    /// <summary>
    /// Verifies that DisplayLatestNewsAsync skips precondition checks and fetches news immediately,
    /// even if LastFetchTime is recent or if it's the first fetch.
    /// </summary>
    [Fact]
    public async Task DisplayLatestNewsAsyncSkipsPreconditionsAndFetchesImmediately()
    {
        const string rssXml = """
                              <?xml version="1.0" encoding="UTF-8"?>
                              <rss version="2.0">
                                <channel>
                                  <title>Test Feed</title>
                                  <item>
                                    <title>Latest News Forced Fetch</title>
                                    <link>https://metalama.net/forced-fetch</link>
                                    <pubDate>Mon, 01 Nov 2025 12:00:00 GMT</pubDate>
                                  </item>
                                </channel>
                              </rss>
                              """;

        this.RegisterResponse( RssClient.BriefsUrl, rssXml );
        this.Time.Set( new DateTime( 2025, 11, 1, 0, 0, 0, DateTimeKind.Utc ) );

        // Set LastFetchTime to very recent (normally would prevent fetching)
        this.UpdateConfiguration( c => c with { LastFetchTime = this.Time.UtcNow.AddMinutes( -30 ), PreferredFeed = RssFeed.Briefs } );

        var rssClient = new RssClient( this.ServiceProvider );

        // DisplayUnreadNewsAsync should not fetch due to recent LastFetchTime
        await rssClient.DisplayUnreadNewsAsync();
        Assert.Empty( this.HttpClientFactory.ProcessedRequests );
        Assert.Empty( this.UserInterface.Notifications );

        // DisplayLatestNewsAsync should skip preconditions and fetch immediately
        await rssClient.DisplayLatestNewsAsync();
        Assert.Single( this.HttpClientFactory.ProcessedRequests );
        var notification = Assert.Single( this.UserInterface.Notifications );
        Assert.Equal( "Latest News Forced Fetch", notification.Title );
        Assert.Equal( "https://metalama.net/forced-fetch?" + WebLinks.TrackingQueryString, notification.Uri );
    }

    /// <summary>
    /// Verifies that DisplayLatestNewsAsync fetches and displays news even on first run (when LastFetchTime is null).
    /// </summary>
    [Fact]
    public async Task DisplayLatestNewsAsyncFetchesOnFirstRun()
    {
        const string rssXml = """
                              <?xml version="1.0" encoding="UTF-8"?>
                              <rss version="2.0">
                                <channel>
                                  <title>Test Feed</title>
                                  <item>
                                    <title>First Run News</title>
                                    <link>https://metalama.net/first-run</link>
                                    <pubDate>Mon, 01 Nov 2025 12:00:00 GMT</pubDate>
                                  </item>
                                </channel>
                              </rss>
                              """;

        this.RegisterResponse( RssClient.BriefsUrl, rssXml );
        this.Time.Set( new DateTime( 2025, 11, 1, 0, 0, 0, DateTimeKind.Utc ) );
        this.UpdateConfiguration( c => c with { PreferredFeed = RssFeed.Briefs } );

        var rssClient = new RssClient( this.ServiceProvider );

        // Verify LastFetchTime is null initially
        var initialConfiguration = this.ConfigurationManager!.Get<RssClientConfiguration>();
        Assert.Null( initialConfiguration.LastFetchTime );

        // DisplayLatestNewsAsync should fetch and display news even though LastFetchTime is null
        await rssClient.DisplayLatestNewsAsync();
        Assert.Single( this.HttpClientFactory.ProcessedRequests );
        var notification = Assert.Single( this.UserInterface.Notifications );
        Assert.Equal( "First Run News", notification.Title );
        Assert.Equal( "https://metalama.net/first-run?" + WebLinks.TrackingQueryString, notification.Uri );

        // Verify LastFetchTime was updated
        var updatedConfiguration = this.ConfigurationManager!.Get<RssClientConfiguration>();
        Assert.NotNull( updatedConfiguration.LastFetchTime );
    }
}