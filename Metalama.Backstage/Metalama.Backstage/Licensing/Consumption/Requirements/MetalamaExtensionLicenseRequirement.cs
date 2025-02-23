// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Licensing.Consumption.Requirements;

public class MetalamaExtensionLicenseRequirement : LicenseRequirement
{
    public MetalamaExtensionLicenseRequirement( string componentName )
    {
        this.ComponentName = componentName;
    }

    public override bool IsEligible( LicenseConsumptionContext context )
    {
        if ( !base.IsEligible( context ) )
        {
            return false;
        }

        // Check that the product is eligible.
        switch ( context.License.LicensedProduct )
        {
            case LicensedProduct.MetalamaProfessional:
            case LicensedProduct.PostSharpFramework:
            case LicensedProduct.PostSharpUltimate:

#pragma warning disable CS0618 // Type or member is obsolete
            case LicensedProduct.MetalamaStarter:
            case LicensedProduct.MetalamaUltimate:
#pragma warning restore CS0618 // Type or member is obsolete
                break;

            default:
                context.Logger.Warning?.Log(
                    $"License '{context.License.DisplayName}' not eligible: the product {context.License.LicensedProduct} is not eligible." );

                return false;
        }

        return true;
    }

    public override string ComponentName { get; }

    public override string RequiredLicenseDescription => "a Metalama Professional license";
}