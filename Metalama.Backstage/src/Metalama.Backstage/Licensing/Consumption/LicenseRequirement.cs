// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Licensing.Consumption.Requirements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Backstage.Licensing.Consumption;

[PublicAPI]
public abstract class LicenseRequirement
{
    public ServicingPhase ServicingPhase { get; }

    protected LicenseRequirement( string componentName, ServicingPhase requiredServicingPhase )
    {
        this.ComponentName = componentName;
        this.ServicingPhase = requiredServicingPhase;
    }

    public virtual bool IsEligible( LicenseConsumptionContext context )
    {
        // Check that we have valid build date.
        var buildDate = context.ApplicationInfo.GetLatestComponentMadeByPostSharp().BuildDate;

        if ( context.License.SubscriptionEndDate != null && buildDate != null && context.License.SubscriptionEndDate < buildDate )
        {
            context.Logger.Warning?.Log( $"License '{context.License.DisplayName}' not eligible: the build date is after the subscription end date." );

            return false;
        }

        if ( context.License.ServicingPhase < this.ServicingPhase )
        {
            context.Logger.Warning?.Log(
                $"License '{context.License.DisplayName}' not eligible: this license qualifies for the {context.License.ServicingPhase.GetDisplayName()} servicing phase, but {this.ServicingPhase.GetDisplayName()} is required for this build." );

            return false;
        }

        var eligibleProducts = this.GetEligibleProducts();

        return eligibleProducts.Count == 0 || eligibleProducts.Contains( context.License.LicenseProduct );
    }

    protected abstract IReadOnlyList<LicenseProduct> GetEligibleProducts();

    public IReadOnlyList<string> EligibleProductNames
        => this.GetEligibleProducts()
            .Where(
                p => p.GetDefaultServicingPhase() >= this.ServicingPhase
                     || (this.ServicingPhase == ServicingPhase.LongTerm && p.CanHaveLongTermSupportOption()) )
            .Select( p => p.GetDisplayName( this.ServicingPhase ) )
            .ToList();

    public string ComponentName { get; }

    public string ComponentNameWithServicingPhase
        => this.ServicingPhase == ServicingPhase.Current ? this.ComponentName : $"{this.ComponentName} ({this.ServicingPhase.GetDisplayName()} Support)";

    public static LicenseRequirement Any => new AnyLicenseRequirement();

    public static LicenseRequirement None => new NoneLicenseRequirement();
}