// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Licensing;

/// <summary>
/// Extension methods of <see cref="ILicenseProductCatalog"/>.
/// </summary>
internal static class LicenseProductCatalogExtensions
{
    /// <summary>
    /// Gets the display name of a product for a given servicing phase, which mentions the long-term support option
    /// when the product has one and the phase requires it.
    /// </summary>
    public static string GetDisplayName( this ILicenseProductCatalog catalog, LicenseProduct product, ServicingPhase servicingPhase )
    {
        if ( servicingPhase is ServicingPhase.LongTerm && catalog.CanHaveLongTermSupportOption( product ) )
        {
            return catalog.GetDisplayName( product ) + " with long-term support";
        }
        else
        {
            return catalog.GetDisplayName( product );
        }
    }
}
