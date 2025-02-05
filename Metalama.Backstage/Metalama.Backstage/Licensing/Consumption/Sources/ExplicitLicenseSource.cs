// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Backstage.Licensing.Consumption.Sources;

internal class ExplicitLicenseSource : LicenseSourceBase
{
    private readonly string _licenseString;

    public override string Description => "the MSBuild property or environment variable named MetalamaLicense";

    public override LicenseSourceKind Kind => LicenseSourceKind.Explicit;

    public ExplicitLicenseSource( string licenseString, IServiceProvider services )
        : base( services )
    {
        this._licenseString = licenseString;
    }

    protected override string? GetLicenseString() => this._licenseString;

    public override LicenseSourcePriority Priority => LicenseSourcePriority.Explicit;
}