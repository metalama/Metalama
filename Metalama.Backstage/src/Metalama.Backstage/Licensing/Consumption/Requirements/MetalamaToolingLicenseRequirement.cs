// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace Metalama.Backstage.Licensing.Consumption.Requirements;

public class MetalamaToolingLicenseRequirement : LicenseRequirement
{
    public MetalamaToolingLicenseRequirement( ServicingPhase requiredServicingPhase = ServicingPhase.Default ) : base(
        "Visual Studio Tools for Metalama",
        requiredServicingPhase ) { }

    public override bool IsEligible( LicenseConsumptionContext context )
    {
        if ( !base.IsEligible( context ) )
        {
            return false;
        }

        // Check that the product is eligible.
        switch ( context.License.LicenseProduct )
        {
            case LicenseProduct.MetalamaCommunity:
            case LicenseProduct.MetalamaProfessional:
            case LicenseProduct.MetalamaEnterprise:
            case LicenseProduct.PostSharpFramework:
            case LicenseProduct.PostSharpUltimate:

#pragma warning disable CS0618 // Type or member is obsolete
            case LicenseProduct.MetalamaStarter:
            case LicenseProduct.MetalamaUltimate:
#pragma warning restore CS0618 // Type or member is obsolete
                break;

            default:
                context.Logger.Warning?.Log(
                    $"License '{context.License.DisplayName}' not eligible: the product {context.License.LicenseProduct} is not eligible." );

                return false;
        }

        return true;
    }

    protected override IReadOnlyList<LicenseProduct> GetEligibleProducts()
        =>
        [
            LicenseProduct.MetalamaCommunity,
            LicenseProduct.MetalamaProfessional,
            LicenseProduct.MetalamaEnterprise,
            LicenseProduct.PostSharpFramework,
            LicenseProduct.PostSharpUltimate,
#pragma warning disable CS0618 // Type or member is obsolete
            LicenseProduct.MetalamaStarter,
            LicenseProduct.MetalamaUltimate
#pragma warning restore CS0618 // Type or member is obsolete
        ];
}