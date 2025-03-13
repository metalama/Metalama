// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;

namespace Metalama.Backstage.Licensing.Consumption.Requirements;

public class MetalamaExtensionLicenseRequirement : LicenseRequirement
{
    public MetalamaExtensionLicenseRequirement( string componentName, ServicingPhase requiredServicingPhase = ServicingPhase.Default ) : base(
        componentName,
        requiredServicingPhase ) { }

    protected override IReadOnlyList<LicenseProduct> GetEligibleProducts()
        =>
        [
            LicenseProduct.MetalamaProfessional,
            LicenseProduct.MetalamaEnterprise,
            LicenseProduct.PostSharpFramework,
            LicenseProduct.PostSharpUltimate,
            LicenseProduct.MetalamaStarter,
            LicenseProduct.MetalamaUltimate
        ];
}