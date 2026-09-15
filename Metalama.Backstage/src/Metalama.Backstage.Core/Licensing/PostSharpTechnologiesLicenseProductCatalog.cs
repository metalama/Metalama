// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Collections.Immutable;

namespace Metalama.Backstage.Licensing;

/// <summary>
/// The catalog of the products of PostSharp Technologies: the members that are common to the Metalama and PostSharp
/// product families, such as the display names of every product. The members that depend on the product family,
/// such as the edition that a trial gives, are implemented by the derived class of each family.
/// </summary>
/// <remarks>
/// This class is defined in the licensing package because <see cref="LicenseProduct"/>, the enumeration of the
/// products that a license key can name, is defined there too.
/// </remarks>
[PublicAPI]
public abstract class PostSharpTechnologiesLicenseProductCatalog : ILicenseProductCatalog
{
#pragma warning disable CS0618 // Type or member is obsolete: the catalog must name the products that are no longer offered.

    /// <inheritdoc />
    public virtual string GetDisplayName( LicenseProduct product )
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
    public virtual string GetLicenseDisplayName( LicenseProduct product, LicenseType licenseType )
        => product switch
        {
            LicenseProduct.MetalamaProfessional => $"Metalama Professional, {licenseType.GetLicenseTypeName()}",
            LicenseProduct.MetalamaUltimate => $"Metalama Ultimate, {licenseType.GetLicenseTypeName()}",
            LicenseProduct.MetalamaStarter => $"Metalama Starter, {licenseType.GetLicenseTypeName()}",
            _ => this.GetDisplayName( product )
        };

    /// <inheritdoc />
    public virtual ServicingPhase GetDefaultServicingPhase( LicenseProduct product )
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
    public virtual bool CanHaveLongTermSupportOption( LicenseProduct product )
        => product switch
        {
            LicenseProduct.PostSharpUltimate => true,
            LicenseProduct.PostSharpFramework => true,
            _ => false
        };

    /// <inheritdoc />
    public abstract bool IsProductOfFamily( LicenseProduct product );

    /// <inheritdoc />
    public abstract bool IsFreeProduct( LicenseProduct product );

    /// <inheritdoc />
    public abstract bool RequiresVersionSpecificRegistration( LicenseProduct product );

    /// <inheritdoc />
    public abstract ImmutableArray<LicenseProduct> GetProductsCoexistingWith( LicenseProduct product );

    /// <inheritdoc />
    public abstract string PremiumEditionDisplayName { get; }

    /// <inheritdoc />
    public abstract LicenseProduct EvaluationProduct { get; }

    /// <inheritdoc />
    public abstract LicenseProduct? CommunityProduct { get; }

    /// <inheritdoc />
    public abstract LicenseProduct? LegacyFreeProduct { get; }

#pragma warning restore CS0618
}
