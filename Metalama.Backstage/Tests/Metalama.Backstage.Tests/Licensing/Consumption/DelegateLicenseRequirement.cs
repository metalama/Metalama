// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Consumption;
using System;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

internal sealed class DelegateLicenseRequirement : LicenseRequirement
{
    private readonly Predicate<LicenseConsumptionContext> _predicate;

    public DelegateLicenseRequirement( Predicate<LicenseConsumptionContext> predicate )
    {
        this._predicate = predicate;
    }

    public override bool IsEligible( LicenseConsumptionContext context ) => this._predicate( context );

    public override string ComponentName => throw new NotImplementedException();

    public override string RequiredLicenseDescription => throw new NotImplementedException();
}