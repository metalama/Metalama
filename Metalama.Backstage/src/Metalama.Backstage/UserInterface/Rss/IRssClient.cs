// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System.Threading.Tasks;

namespace Metalama.Backstage.UserInterface.Rss;

public interface IRssClient : IBackstageService
{
    void Initialize();

    Task DisplayLatestNewsAsync();

    void Disable();

    /// <summary>
    /// Enables the news feed by setting the preferred feed to <see cref="RssFeed.Briefs"/>, unless telemetry is disabled,
    /// in which case the news feed cannot be enabled (because it would otherwise contact <c>metalama.net</c>). See issue #1670.
    /// </summary>
    /// <returns><c>true</c> if the news feed was enabled, or <c>false</c> if it could not be enabled because telemetry is disabled.</returns>
    bool TryEnable();
}