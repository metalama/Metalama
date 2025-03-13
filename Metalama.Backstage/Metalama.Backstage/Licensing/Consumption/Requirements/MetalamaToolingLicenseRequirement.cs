// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
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
            LicenseProduct.MetalamaStarter,
            LicenseProduct.MetalamaUltimate
        ];
}