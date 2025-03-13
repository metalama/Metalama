// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Registration;
using System;

namespace Metalama.Backstage.Licensing.Licenses
{
    /// <summary>
    /// Provides extension methods for processing license key data for license consumption, registration and audit.
    /// </summary>
    internal static class LicenseKeyDataExtensions
    {
        /// <summary>
        /// If the <paramref name="licenseKeyData"/> contains an obsolete license type, it gets transformed to a respective non-obsolete one.
        /// Otherwise, the same license type is returned.
        /// </summary>
        internal static LicenseType NormalizeLicenseType( this LicenseKeyData licenseKeyData )
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if ( licenseKeyData is { Product: LicenseProduct.PostSharpUltimate1, LicenseType: LicenseType.Professional } )
#pragma warning restore CS0618 // Type or member is obsolete
            {
                return LicenseType.Business;
            }
            else
            {
                return licenseKeyData.LicenseType;
            }
        }

#pragma warning disable CS0612, CS0618 // Type or member is obsolete
        internal static LicenseProduct NormalizeProduct( this LicenseKeyData licenseKeyData )
            => (licenseKeyData.Product, licenseKeyData.LicenseType) switch
            {
                (LicenseProduct.PostSharpUltimate1, LicenseType.Professional) => LicenseProduct.PostSharpFramework,
                (LicenseProduct.PostSharpUltimate1 or LicenseProduct.PostSharpUltimate, LicenseType.Essentials) => LicenseProduct.PostSharpEssentials,
                (LicenseProduct.PostSharpUltimate1, _) => LicenseProduct.PostSharpUltimate,
                _ => licenseKeyData.Product
            };
#pragma warning restore CS0618

        internal static ServicingPhase NormalizeServicingPhase( this LicenseKeyData licenseKeyData )
            => licenseKeyData.Generation.GetValueOrDefault() == LicenseGeneration.None
                ? ServicingPhase.LongTerm
                : licenseKeyData.SupportLevel ?? licenseKeyData.Product switch
                {
                    // Note that Metalama Enterprise is Metalama Professional with a ServicingPhase field set to LongTerm.
                    LicenseProduct.MetalamaProfessional => ServicingPhase.Extended,
                    LicenseProduct.MetalamaEnterprise => ServicingPhase.LongTerm,
                    LicenseProduct.PostSharpFramework => ServicingPhase.Extended,
                    LicenseProduct.PostSharpUltimate => ServicingPhase.Extended,
                    _ => ServicingPhase.Default
                };

        internal static string GetDisplayName( this LicenseKeyData licenseKeyData )
        {
            var product = NormalizeProduct( licenseKeyData );

            return product switch
            {
                LicenseProduct.MetalamaProfessional => $"Metalama Professional, {licenseKeyData.LicenseType.GetLicenseTypeName()}",
#pragma warning disable CS0618 // Type or member is obsolete
                LicenseProduct.MetalamaUltimate => $"Metalama Ultimate, {licenseKeyData.LicenseType.GetLicenseTypeName()}",
                LicenseProduct.MetalamaStarter => $"Metalama Starter, {licenseKeyData.LicenseType.GetLicenseTypeName()}",
#pragma warning restore CS0618 // Type or member is obsolete
                LicenseProduct.None => "Metalama Open Source",
                _ => product.GetDisplayName()
            };
        }

        internal static Version GetMinPostSharpVersion( this LicenseKeyData licenseKeyData )
        {
#pragma warning disable 618

            // This logic is for PostSharp versions before 6.9.3.
            // The later versions are forward compatible without the need of updating of this logic.
            // Products not based on PostSharp (e.g. Metalama) don't need this logic at all.

            if ( licenseKeyData.MinPostSharpVersion != null )
            {
                return licenseKeyData.MinPostSharpVersion;
            }
            else if ( licenseKeyData.LicenseType == LicenseType.PerUsage || licenseKeyData.Product == LicenseProduct.PostSharpCachingLibrary )
            {
                return new Version( 6, 6, 0 );
            }
            else if ( licenseKeyData.Product == LicenseProduct.PostSharp20 )
            {
                return new Version( 2, 0, 0 );
            }
            else if ( licenseKeyData.Product is LicenseProduct.PostSharpUltimate or LicenseProduct.PostSharpFramework
                      && licenseKeyData.LicenseType == LicenseType.Enterprise )
            {
                return new Version( 5, 0, 22 );
            }
            else if ( licenseKeyData.LicenseServerEligible != null )
            {
                return new Version( 5, 0, 22 );
            }
            else
            {
                return new Version( 3, 0, 0 );
            }
#pragma warning restore 618
        }

        /// <summary>
        /// Creates a new object of <see cref="LicenseRegistrationProperties"/> based on the given <see cref="LicenseKeyData"/>.
        /// </summary>
        public static LicenseRegistrationProperties ToLicenseRegistrationProperties( this LicenseKeyData licenseKeyData, string? licenseString = null )
        {
            var description = licenseKeyData.GetDisplayName();

            bool licenseServerEligible;

            if ( licenseKeyData.LicenseServerEligible.HasValue )
            {
                licenseServerEligible = licenseKeyData.LicenseServerEligible.Value;
            }
            else
            {
                const int lastLicenseIdBefore50Rtm = 100802;
                licenseServerEligible = licenseKeyData.LicenseId is > 0 and <= lastLicenseIdBefore50Rtm;
            }

            var auditable = licenseKeyData.LicenseType switch
            {
#pragma warning disable CS0618 // Type or member is obsolete
                LicenseType.Site or LicenseType.Global or LicenseType.Anonymous => false,
#pragma warning restore CS0618                  // Type or member is obsolete
                LicenseType.Evaluation => true, // We want to audit evaluation licenses so we know how people are using the product during evaluation.
                _ => licenseKeyData.Auditable ?? true
            };

            LicenseRegistrationProperties data = new(
                licenseKeyData.LicenseString ?? licenseString ?? throw new ArgumentNullException(
                    nameof(licenseString),
                    "'licenseString' cannot be null if LicenseKeyData.LicenseString is null." ),
                licenseKeyData.LicenseUniqueId,
                licenseKeyData.LicenseGuid != null,
                licenseKeyData.LicenseGuid == null ? licenseKeyData.LicenseId : null,
                licenseKeyData.Licensee,
                description,
                licenseKeyData.NormalizeProduct(),
                licenseKeyData.NormalizeLicenseType(),
                licenseKeyData.ValidFrom,
                licenseKeyData.ValidTo,
                !licenseKeyData.ValidTo.HasValue,
                licenseKeyData.SubscriptionEndDate,
                auditable,
                licenseServerEligible,
                licenseKeyData.GetMinPostSharpVersion(),
                licenseKeyData.Generation.GetValueOrDefault(),
                licenseKeyData.NormalizeServicingPhase() );

            return data;
        }
    }
}