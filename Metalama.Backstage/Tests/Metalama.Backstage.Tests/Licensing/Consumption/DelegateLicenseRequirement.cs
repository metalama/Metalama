// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Consumption;
using System;
using System.Collections.Generic;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

internal sealed class DelegateLicenseRequirement : LicenseRequirement
{
    private readonly Predicate<LicenseConsumptionContext> _predicate;

    public DelegateLicenseRequirement( Predicate<LicenseConsumptionContext> predicate ) : base( "<Component>", ServicingPhase.Default )
    {
        this._predicate = predicate;
    }

    public override bool IsEligible( LicenseConsumptionContext context ) => this._predicate( context );

    protected override IReadOnlyList<LicenseProduct> GetEligibleProducts() => [];
}