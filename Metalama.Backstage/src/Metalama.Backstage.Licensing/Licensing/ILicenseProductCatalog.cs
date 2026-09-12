// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System.Collections.Immutable;

namespace Metalama.Backstage.Licensing;

/// <summary>
/// Describes the products that a product family sells and the business rules attached to them: display names, default
/// servicing phases, which products a license key of the family may name, and which products the unsigned licenses
/// (community, evaluation) are issued for. The host product supplies the implementation, because these rules belong
/// to the vendor and not to the licensing services.
/// </summary>
/// <remarks>
/// The wire format of a license key identifies a product by a byte, represented by <see cref="LicenseProduct"/>. The
/// catalog gives that byte its meaning for the current product family.
/// </remarks>
[PublicAPI]
public interface ILicenseProductCatalog : IBackstageService
{
    /// <summary>
    /// Gets the display name of a product, for instance <c>Metalama Professional</c>.
    /// </summary>
    string GetDisplayName( LicenseProduct product );

    /// <summary>
    /// Gets the display name of a license key of a given product and type, for instance
    /// <c>Metalama Professional, Business License</c>.
    /// </summary>
    string GetLicenseDisplayName( LicenseProduct product, LicenseType licenseType );

    /// <summary>
    /// Gets the servicing phase that a license key of a product qualifies for when the key does not specify one.
    /// </summary>
    ServicingPhase GetDefaultServicingPhase( LicenseProduct product );

    /// <summary>
    /// Determines whether a product can be sold with a long-term support option.
    /// </summary>
    bool CanHaveLongTermSupportOption( LicenseProduct product );

    /// <summary>
    /// Determines whether a license key of a given product can be consumed by the current product family at all. A
    /// key of another product is rejected with a message before any requirement is evaluated.
    /// </summary>
    bool IsProductOfFamily( LicenseProduct product );

    /// <summary>
    /// Determines whether a product is a free edition, which has no subscription to renew.
    /// </summary>
    bool IsFreeProduct( LicenseProduct product );

    /// <summary>
    /// Determines whether a registered license key of a product must be stored in the group of keys that only the
    /// versions supporting that product read, rather than in the legacy location that every version reads.
    /// </summary>
    bool RequiresVersionSpecificRegistration( LicenseProduct product );

    /// <summary>
    /// Gets the products whose registered license keys are kept when a license key of a given product is registered.
    /// The keys of every other product are removed. The result is empty when no key is kept.
    /// </summary>
    ImmutableArray<LicenseProduct> GetProductsCoexistingWith( LicenseProduct product );

    /// <summary>
    /// Gets the display name of the edition that the user is invited to try or to buy when a component is not
    /// licensed, for instance <c>Metalama Professional</c>.
    /// </summary>
    string PremiumEditionDisplayName { get; }

    /// <summary>
    /// Gets the product for which an evaluation license is issued.
    /// </summary>
    LicenseProduct EvaluationProduct { get; }

    /// <summary>
    /// Gets the product for which a community license is issued, or <c>null</c> when the family has no community
    /// edition.
    /// </summary>
    LicenseProduct? CommunityProduct { get; }

    /// <summary>
    /// Gets the product for which the legacy free license is issued, or <c>null</c> when the family has no such
    /// edition.
    /// </summary>
    LicenseProduct? LegacyFreeProduct { get; }
}
