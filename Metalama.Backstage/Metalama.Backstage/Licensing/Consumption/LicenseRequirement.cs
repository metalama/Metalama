// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing.Consumption.Requirements;

namespace Metalama.Backstage.Licensing.Consumption;

[PublicAPI]
public abstract class LicenseRequirement
{
    public virtual bool IsEligible( LicenseConsumptionContext context )
    {
        // Check that we have valid build date.
        var buildDate = context.ApplicationInfo.GetLatestComponentMadeByPostSharp().BuildDate;

        if ( context.License.SubscriptionEndDate != null && buildDate != null && context.License.SubscriptionEndDate < buildDate )
        {
            context.Logger.Warning?.Log( $"License '{context.License.DisplayName}' not eligible: the build date is after the subscription end date." );

            return false;
        }

        return true;
    }
    
    public abstract string ComponentName { get; }
    public abstract string RequiredLicenseDescription { get; }

    public static LicenseRequirement Any => new AnyLicenseRequirement();
}