// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;

namespace Metalama.Backstage.UserInterface;

/// <summary>
/// Provides the addresses of the web pages of the product that the user interface opens. The host product supplies the
/// implementation, because the addresses belong to the product and not to the Backstage services.
/// </summary>
[PublicAPI]
public interface IWebLinks : IBackstageService
{
    /// <summary>
    /// Gets the query string that identifies the product user interface as the origin of a visit, or an empty string
    /// when visits are not tracked. It is appended to the links of the news feed.
    /// </summary>
    string TrackingQueryString { get; }

    /// <summary>
    /// Gets the address of the page shown after the first activation of the product.
    /// </summary>
    string Welcome { get; }

    /// <summary>
    /// Gets the address of the page from which a team can request an evaluation license.
    /// </summary>
    string GetTeamTeamTrial { get; }

    /// <summary>
    /// Gets the address of the extension of the product in the Visual Studio Marketplace.
    /// </summary>
    string VisualStudioMarketplace { get; }

    /// <summary>
    /// Gets the address of the privacy policy.
    /// </summary>
    string PrivacyPolicy { get; }

    /// <summary>
    /// Gets the address of the license agreement.
    /// </summary>
    string LicenseAgreement { get; }

    /// <summary>
    /// Gets the address of the documentation.
    /// </summary>
    string Documentation { get; }

    /// <summary>
    /// Gets the address of the page that explains how to install the IDE extension of the product.
    /// </summary>
    string InstallVsx { get; }

    /// <summary>
    /// Gets the address of the page from which a subscription is renewed.
    /// </summary>
    string RenewSubscription { get; }

    /// <summary>
    /// Gets the address of the page that documents the command-line tool of the product.
    /// </summary>
    string DotNetTool { get; }

    /// <summary>
    /// Gets the address of the page that explains how to disable telemetry.
    /// </summary>
    string DisableTelemetryInstructions { get; }
}
