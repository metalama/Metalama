// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Backstage.Licensing.Consumption.Requirements;

internal sealed class AnyLicenseRequirement : LicenseRequirement
{
    public override bool IsEligible( LicenseConsumptionContext context ) => true;

    public override string ComponentName => "Any";

    public override string RequiredLicenseDescription => "any license";
}