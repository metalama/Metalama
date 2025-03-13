// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace Metalama.Backstage.Licensing.Consumption.Requirements;

internal sealed class AnyLicenseRequirement : LicenseRequirement
{
    public AnyLicenseRequirement() : base( "Any", ServicingPhase.Default ) { }

    public override bool IsEligible( LicenseConsumptionContext context ) => true;

    protected override IReadOnlyList<LicenseProduct> GetEligibleProducts()
        =>
        [
            LicenseProduct.MetalamaCommunity, LicenseProduct.MetalamaEnterprise,
            LicenseProduct.MetalamaProfessional, LicenseProduct.PostSharpEssentials, LicenseProduct.PostSharpFramework, LicenseProduct.PostSharpUltimate,
            LicenseProduct.PostSharpCachingLibrary, LicenseProduct.PostSharpDiagnosticsLibrary, LicenseProduct.PostSharpModelLibrary,
            LicenseProduct.PostSharpThreadingLibrary, LicenseProduct.None,
#pragma warning disable CS0618 // Type or member is obsolete
            LicenseProduct.MetalamaFree, LicenseProduct.MetalamaStarter,
            LicenseProduct.MetalamaUltimate
#pragma warning restore CS0618 // Type or member is obsolete
        ];
}