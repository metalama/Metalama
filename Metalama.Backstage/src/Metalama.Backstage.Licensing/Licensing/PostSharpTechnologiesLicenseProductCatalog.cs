// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Collections.Immutable;

namespace Metalama.Backstage.Licensing;

/// <summary>
/// The catalog of the products of PostSharp Technologies, as consumed by the Metalama product family: the Metalama
/// editions, and the PostSharp editions whose license keys are also valid for Metalama.
/// </summary>
/// <remarks>
/// This class describes the products of one company. It is defined in the licensing package because
/// <see cref="LicenseProduct"/>, the enumeration of the products that a license key can name, is defined there too;
/// both belong to a package of the company once the enumeration is no longer part of the license key format.
/// </remarks>
[PublicAPI]
public sealed class PostSharpTechnologiesLicenseProductCatalog : ILicenseProductCatalog
{
    /// <summary>
    /// Gets the single instance of the catalog.
    /// </summary>
    public static PostSharpTechnologiesLicenseProductCatalog Instance { get; } = new();

    private PostSharpTechnologiesLicenseProductCatalog() { }

#pragma warning disable CS0618 // Type or member is obsolete: the catalog must name the products that are no longer offered.

    /// <inheritdoc />
    public string GetDisplayName( LicenseProduct product )
        => product switch
        {
            LicenseProduct.MetalamaCommunity => "Metalama Community",
            LicenseProduct.MetalamaEnterprise => "Metalama Enterprise",
            LicenseProduct.MetalamaProfessional => "Metalama Professional",
            LicenseProduct.PostSharpEssentials => "PostSharp Essentials",
            LicenseProduct.PostSharpFramework => "PostSharp Framework",
            LicenseProduct.PostSharpUltimate => "PostSharp Ultimate",
            LicenseProduct.PostSharpCachingLibrary => "PostSharp Caching",
            LicenseProduct.PostSharpDiagnosticsLibrary => "PostSharp Logging",
            LicenseProduct.PostSharpModelLibrary => "PostSharp MVVM",
            LicenseProduct.PostSharpThreadingLibrary => "PostSharp Threading",
            LicenseProduct.MetalamaFree => "Metalama Free (legacy)",
            LicenseProduct.MetalamaStarter => "Metalama Starter (legacy)",
            LicenseProduct.MetalamaUltimate => "Metalama Ultimate (legacy)",
            _ => product.ToString()
        };

    /// <inheritdoc />
    public string GetLicenseDisplayName( LicenseProduct product, LicenseType licenseType )
        => product switch
        {
            LicenseProduct.MetalamaProfessional => $"Metalama Professional, {licenseType.GetLicenseTypeName()}",
            LicenseProduct.MetalamaUltimate => $"Metalama Ultimate, {licenseType.GetLicenseTypeName()}",
            LicenseProduct.MetalamaStarter => $"Metalama Starter, {licenseType.GetLicenseTypeName()}",
            LicenseProduct.None => "Metalama Open Source",
            _ => this.GetDisplayName( product )
        };

    /// <inheritdoc />
    public ServicingPhase GetDefaultServicingPhase( LicenseProduct product )
        => product switch
        {
            // Metalama Enterprise is Metalama Professional with a ServicingPhase field set to LongTerm.
            LicenseProduct.MetalamaProfessional => ServicingPhase.Extended,
            LicenseProduct.MetalamaEnterprise => ServicingPhase.LongTerm,
            LicenseProduct.PostSharpFramework => ServicingPhase.Extended,
            LicenseProduct.PostSharpUltimate => ServicingPhase.Extended,
            _ => ServicingPhase.Current
        };

    /// <inheritdoc />
    public bool CanHaveLongTermSupportOption( LicenseProduct product )
        => product switch
        {
            LicenseProduct.PostSharpUltimate => true,
            LicenseProduct.PostSharpFramework => true,
            _ => false
        };

    /// <inheritdoc />
    public bool IsProductOfFamily( LicenseProduct product )
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
    public bool IsFreeProduct( LicenseProduct product ) => product is LicenseProduct.MetalamaCommunity or LicenseProduct.MetalamaFree;

    /// <inheritdoc />
    /// <remarks>
    /// Metalama Community was introduced in Metalama 2025.1, so its keys are stored where earlier versions do not read.
    /// </remarks>
    public bool RequiresVersionSpecificRegistration( LicenseProduct product ) => product is LicenseProduct.MetalamaCommunity;

    /// <inheritdoc />
    /// <remarks>
    /// Metalama Community and Metalama Free co-exist for backward compatibility: a version that supports only one of
    /// them keeps consuming its own key.
    /// </remarks>
    public ImmutableArray<LicenseProduct> GetProductsCoexistingWith( LicenseProduct product )
        => product switch
        {
            LicenseProduct.MetalamaCommunity => ImmutableArray.Create( LicenseProduct.MetalamaFree ),
            LicenseProduct.MetalamaFree => ImmutableArray.Create( LicenseProduct.MetalamaCommunity ),
            _ => ImmutableArray<LicenseProduct>.Empty
        };

    /// <inheritdoc />
    public string PremiumEditionDisplayName => "Metalama Professional";

    /// <inheritdoc />
    public LicenseProduct EvaluationProduct => LicenseProduct.MetalamaProfessional;

    /// <inheritdoc />
    public LicenseProduct? CommunityProduct => LicenseProduct.MetalamaCommunity;

    /// <inheritdoc />
    public LicenseProduct? LegacyFreeProduct => LicenseProduct.MetalamaFree;

#pragma warning restore CS0618
}
