// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using System;
using System.ComponentModel;

namespace Metalama.Backstage.UserInterface.Rss;

[ConfigurationFile( "rss.json" )]
[Description( "Toast notifications for product news." )]
public sealed record RssClientConfiguration : ConfigurationFile
{
    internal DateTime? LastFetchTime { get; init; }

    public RssFeed? PreferredFeed { get; init; }
}