// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.Commands.Rss;

public abstract class RssCommand : BaseCommand<BaseCommandSettings>
{
    // Reading the configuration needs no user interface, and a status command must never trigger a news fetch/toast as a
    // side effect of process startup.
    protected override BackstageInitializationOptions AddBackstageOptions( BackstageInitializationOptions options ) => options with { AddRssClient = true };
}