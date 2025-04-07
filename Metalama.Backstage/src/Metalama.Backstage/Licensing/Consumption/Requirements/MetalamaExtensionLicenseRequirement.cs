// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Generic;

namespace Metalama.Backstage.Licensing.Consumption.Requirements;

public class MetalamaExtensionLicenseRequirement : LicenseRequirement
{
    public MetalamaExtensionLicenseRequirement( string componentName, ServicingPhase requiredServicingPhase = ServicingPhase.Current ) : base(
        componentName,
        requiredServicingPhase ) { }

    protected override IReadOnlyList<LicenseProduct> GetEligibleProducts()
        =>
        [
            LicenseProduct.MetalamaProfessional,
            LicenseProduct.MetalamaEnterprise,
            LicenseProduct.PostSharpFramework,
            LicenseProduct.PostSharpUltimate,
#pragma warning disable CS0618 // Type or member is obsolete
            LicenseProduct.MetalamaStarter,
            LicenseProduct.MetalamaUltimate
#pragma warning restore CS0618 // Type or member is obsolete
        ];
}