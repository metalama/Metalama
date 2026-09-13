// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.UserInterface;

/// <summary>
/// The addresses that the user interface services use. The host product supplies them, because they belong to the
/// product and not to the Backstage services.
/// </summary>
[PublicAPI]
public sealed record UserInterfaceInitializationOptions : IBackstageService
{
    /// <summary>
    /// Gets a value indicating whether the welcome web page should be opened the first time telemetry is activated.
    /// The default is <c>false</c>. The command-line compiler enables it; Visual Studio leaves it disabled because the
    /// extension shows its own welcome page. See #1701.
    /// </summary>
    public bool OpenWelcomePage { get; init; }

    /// <summary>
    /// Gets a value indicating whether the notifications that depend on the state of the machine (a missing license,
    /// an expiring subscription, a missing IDE extension) should be detected and shown. The default is <c>false</c>.
    /// </summary>
    public bool DetectToastNotifications { get; init; }

    /// <summary>
    /// Gets a value indicating whether the news client should be registered. The default is <c>false</c>.
    /// </summary>
    public bool AddRssClient { get; init; }

    /// <summary>
    /// Gets the address of the RSS feed of the short news of the product, or <c>null</c> when the product has no such
    /// feed.
    /// </summary>
    public string? BriefsFeedUrl { get; init; }

    /// <summary>
    /// Gets the address of the RSS feed of the articles of the product, or <c>null</c> when the product has no such
    /// feed.
    /// </summary>
    public string? PostsFeedUrl { get; init; }
}
