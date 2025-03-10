// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Licenses;

namespace Metalama.Backstage.Licensing.Consumption.Requirements;

public class MetalamaToolingLicenseRequirement : LicenseRequirement
{
    private MetalamaToolingLicenseRequirement() { }

    public static LicenseRequirement Instance { get; } = new MetalamaToolingLicenseRequirement();

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

        // Check that the subscription is still active or grace.
        if ( context.License is
            { Generation: >= LicenseGeneration.V20251, SubscriptionStatus: SubscriptionStatus.Expired } )
        {
            context.Logger.Warning?.Log( $"License '{context.License.DisplayName}' not eligible: subscription has expired." );

            return false;
        }

        return true;
    }

    public override string ComponentName => "Visual Studio Tools for Metalama";

    public override string RequiredLicenseDescription => "Metalama Community or Metalama Professional with an active subscription";
}