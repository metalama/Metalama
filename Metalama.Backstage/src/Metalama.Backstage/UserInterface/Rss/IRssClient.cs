// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Telemetry;
using System.Threading.Tasks;

namespace Metalama.Backstage.UserInterface.Rss;

public interface IRssClient : IBackstageService
{
    Task DisplayUnreadLatestNewsAsync( ITelemetryContext context );

    Task<bool> DisplayLatestNewsAsync();

    void Disable();

    /// <summary>
    /// Enables the news feed by setting the preferred feed to <see cref="RssFeed.Briefs"/>, unless telemetry is disabled.
    /// When telemetry is disabled, the feed is not enabled, because the daily fetch from <c>metalama.net</c> is gated on the
    /// telemetry setting and would never run, so enabling the feed would have no effect. See issue #1670.
    /// </summary>
    /// <returns><c>true</c> if the news feed was enabled, or <c>false</c> if it was not enabled because telemetry is disabled.</returns>
    bool TryEnable();
}