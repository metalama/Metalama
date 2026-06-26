// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.UserInterface.Rss;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Tools.Config.Tests.Commands.Rss
{
    public sealed class RssStatusCommandTests : CommandsTestsBase
    {
        public RssStatusCommandTests( ITestOutputHelper logger )
            : base( logger ) { }

        [Fact]
        public async Task Status_WithDefaultConfiguration_ReportsEnabled()
        {
            // An unset preferred feed defaults to Briefs, so the feed is enabled. We assert short, wrap-safe substrings
            // because a Spectre table can wrap long cells across lines.
            await this.TestCommandAsync( "news status", "Enabled" );
            await this.TestCommandAsync( "news status", "Briefs" );
        }

        [Fact]
        public async Task Status_WhenDisabled_ReportsDisabled()
        {
            this.ConfigurationManager!.Update<RssClientConfiguration>( c => c with { PreferredFeed = RssFeed.None } );

            await this.TestCommandAsync( "news status", "Disabled" );
        }

        [Fact]
        public async Task Status_WhenPostsSelected_ReportsPosts()
        {
            this.ConfigurationManager!.Update<RssClientConfiguration>( c => c with { PreferredFeed = RssFeed.Posts } );

            await this.TestCommandAsync( "news status", "Posts" );
        }
    }
}
