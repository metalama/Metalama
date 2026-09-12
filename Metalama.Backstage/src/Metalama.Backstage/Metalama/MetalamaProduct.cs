// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Application;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Telemetry;
using Metalama.Backstage.UserInterface;
using System;
using System.Collections.Immutable;
using System.IO;

namespace Metalama.Backstage;

/// <summary>
/// The values that configure the Backstage services for the Metalama product family: the product profile, the web
/// links, the telemetry endpoints and the news feeds.
/// </summary>
/// <remarks>
/// This class is the default of the corresponding properties of <see cref="Extensibility.BackstageInitializationOptions"/>.
/// It belongs to the Metalama host and not to the product-neutral services, and it is defined in this assembly only
/// until the services are extracted into their own packages. A host of another product supplies its own values.
/// </remarks>
[PublicAPI]
public static class MetalamaProduct
{
    /// <summary>
    /// The address of the RSS feed of the short Metalama news.
    /// </summary>
    public const string BriefsFeedUrl = "https://metalama.net/briefs.xml";

    /// <summary>
    /// The address of the RSS feed of the Metalama articles.
    /// </summary>
    public const string PostsFeedUrl = "https://metalama.net/feed.xml";

    /// <summary>
    /// Gets the profile of the Metalama product family. Its values are the names that every version of Metalama has
    /// used on the machine, so they must not change.
    /// </summary>
    public static ProductProfile Profile { get; } = new(
        Name: "Metalama",
        Company: "PostSharp Technologies",
        DataDirectoryName: "Metalama",
        EnvironmentVariablePrefix: "METALAMA_",
        GlobalLockNamePrefix: "Global\\Metalama_",
        LicensePropertyName: "MetalamaLicense",
        AssemblyNamePrefix: "Metalama" )
    {
        LegacyDataDirectoryName = ".metalama",
        RepositoryConfigurationFileName = "metalama.json",
        TrustedAssemblyNamePrefixes = ImmutableArray.Create( "PostSharp", "Metalama" )
    };

    /// <summary>
    /// Gets the catalog of the products whose license keys Metalama consumes.
    /// </summary>
    public static ILicenseProductCatalog LicenseProductCatalog { get; } = PostSharpTechnologiesLicenseProductCatalog.Instance;

    /// <summary>
    /// Gets the web links of Metalama.
    /// </summary>
    public static IWebLinks WebLinks { get; } = new WebLinks();

    /// <summary>
    /// Gets the telemetry endpoints of Metalama.
    /// </summary>
    public static TelemetryInitializationOptions TelemetryOptions { get; } = new(
        new Uri( "https://bits.postsharp.net:44301/upload" ),
        GetUploadEncryptionPublicKey )
    {
        AnalyticsUri = new Uri( "https://postsharp.matomo.cloud/matomo.php?idsite=6" )
    };

    /// <summary>
    /// Gets the user interface addresses of Metalama.
    /// </summary>
    public static UserInterfaceInitializationOptions UserInterfaceOptions { get; } = new()
    {
        BriefsFeedUrl = BriefsFeedUrl, PostsFeedUrl = PostsFeedUrl
    };

    /// <summary>
    /// Reads the public key that encrypts the telemetry packages from the resources of the current assembly.
    /// </summary>
    /// <returns>The public key, in the <c>RSAKeyValue</c> XML format.</returns>
    private static byte[] GetUploadEncryptionPublicKey()
    {
        using var keyStream = typeof(MetalamaProduct).Assembly.GetManifestResourceStream( "Metalama.Backstage.Telemetry.public.key" )
                              ?? throw new InvalidOperationException( "The public key that encrypts the telemetry packages was not found." );

        using var memoryStream = new MemoryStream();
        keyStream.CopyTo( memoryStream );

        return memoryStream.ToArray();
    }
}
