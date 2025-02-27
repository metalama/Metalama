// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Backstage.Licensing.Consumption.Requirements;

internal sealed class AnyLicenseRequirement : LicenseRequirement
{
    public override bool IsEligible( LicenseConsumptionContext context ) => true;

    public override string ComponentName => "Any";

    public override string RequiredLicenseDescription => "any license";
}