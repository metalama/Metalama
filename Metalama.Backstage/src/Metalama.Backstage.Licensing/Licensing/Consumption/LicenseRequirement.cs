// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Application;
using Metalama.Backstage.Licensing.Consumption.Requirements;
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
        var buildDate = context.ApplicationInfo.GetLatestVendorComponent( context.ProductProfile.Company ).BuildDate;

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

    /// <summary>
    /// Gets the display names of the eligible products, as named by the catalog of PostSharp Technologies. This
    /// property exists for compatibility; <see cref="GetEligibleProductNames"/> takes the catalog of the current
    /// product family.
    /// </summary>
    public IReadOnlyList<string> EligibleProductNames => this.GetEligibleProductNames( PostSharpTechnologiesLicenseProductCatalog.Instance );

    /// <summary>
    /// Gets the display names of the products that satisfy the current requirement, as named by a given catalog.
    /// </summary>
    public IReadOnlyList<string> GetEligibleProductNames( ILicenseProductCatalog catalog )
        => this.GetEligibleProducts()
            .Where(
                p => catalog.GetDefaultServicingPhase( p ) >= this.ServicingPhase
                     || (this.ServicingPhase == ServicingPhase.LongTerm && catalog.CanHaveLongTermSupportOption( p )) )
            .Select( p => catalog.GetDisplayName( p, this.ServicingPhase ) )
            .ToList();

    public string ComponentName { get; }

    public string ComponentNameWithServicingPhase
        => this.ServicingPhase == ServicingPhase.Current ? this.ComponentName : $"{this.ComponentName} ({this.ServicingPhase.GetDisplayName()} Support)";

    public static LicenseRequirement Any => new AnyLicenseRequirement();

    public static LicenseRequirement None => new NoneLicenseRequirement();
}