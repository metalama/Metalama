// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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