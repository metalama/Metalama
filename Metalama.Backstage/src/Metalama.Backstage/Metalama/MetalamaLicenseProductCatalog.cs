// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing;
using System.Collections.Immutable;

namespace Metalama.Backstage;

/// <summary>
/// The catalog of the products of PostSharp Technologies as consumed by the Metalama product family: the Metalama
/// editions, and the PostSharp editions whose license keys are also valid for Metalama.
/// </summary>
[PublicAPI]
public sealed class MetalamaLicenseProductCatalog : LicenseProductCatalog
{
    /// <summary>
    /// Gets the single instance of the catalog.
    /// </summary>
    public static MetalamaLicenseProductCatalog Instance { get; } = new();

    private MetalamaLicenseProductCatalog() { }

#pragma warning disable CS0618 // Type or member is obsolete: the catalog must name the products that are no longer offered.

    /// <inheritdoc />
    public override string GetLicenseDisplayName( LicenseProduct product, LicenseType licenseType )
        => product == LicenseProduct.None ? "Metalama Open Source" : base.GetLicenseDisplayName( product, licenseType );

    /// <inheritdoc />
    public override bool IsProductOfFamily( LicenseProduct product )
        => product switch
        {
            LicenseProduct.MetalamaCommunity => true,
            LicenseProduct.MetalamaProfessional => true,
            LicenseProduct.MetalamaEnterprise => true,
            LicenseProduct.PostSharpFramework => true,
            LicenseProduct.PostSharpUltimate => true,

            // No longer issued but existing keys are fully supported.
            LicenseProduct.MetalamaUltimate => true,
            LicenseProduct.MetalamaStarter => true,
            LicenseProduct.MetalamaFree => true,
            _ => false
        };

    /// <inheritdoc />
    public override bool IsFreeProduct( LicenseProduct product ) => product is LicenseProduct.MetalamaCommunity or LicenseProduct.MetalamaFree;

    /// <inheritdoc />
    /// <remarks>
    /// Metalama Community was introduced in Metalama 2025.1, so its keys are stored where earlier versions do not read.
    /// </remarks>
    public override bool RequiresVersionSpecificRegistration( LicenseProduct product ) => product is LicenseProduct.MetalamaCommunity;

    /// <inheritdoc />
    /// <remarks>
    /// Metalama Community and Metalama Free co-exist for backward compatibility: a version that supports only one of
    /// them keeps consuming its own key.
    /// </remarks>
    public override ImmutableArray<LicenseProduct> GetProductsCoexistingWith( LicenseProduct product )
        => product switch
        {
            LicenseProduct.MetalamaCommunity => ImmutableArray.Create( LicenseProduct.MetalamaFree ),
            LicenseProduct.MetalamaFree => ImmutableArray.Create( LicenseProduct.MetalamaCommunity ),
            _ => ImmutableArray<LicenseProduct>.Empty
        };

    /// <inheritdoc />
    public override string PremiumEditionDisplayName => "Metalama Professional";

    /// <inheritdoc />
    public override LicenseProduct EvaluationProduct => LicenseProduct.MetalamaProfessional;

    /// <inheritdoc />
    public override LicenseProduct? CommunityProduct => LicenseProduct.MetalamaCommunity;

    /// <inheritdoc />
    public override LicenseProduct? LegacyFreeProduct => LicenseProduct.MetalamaFree;

#pragma warning restore CS0618
}
