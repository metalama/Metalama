// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Registration;
using System;
using System.Collections.Generic;

namespace Metalama.Backstage.Licensing.Consumption.Sources;

internal sealed class ExplicitLicenseSource : LicenseSourceBase
{
    private readonly string _licenseString;

    public override string Description => "the MSBuild property or environment variable named MetalamaLicense";

    public override LicenseSourceKind Kind { get; }

    protected override IEnumerable<LicenseRegistrationProperties> GetRegisteredLicenses( Action<LicensingMessage> reportMessage )
    {
        if ( !LicenseKeyData.TryDeserialize( this._licenseString, out var license, out var errorMessage ) )
        {
            reportMessage( new LicensingMessage( errorMessage ) );
        }

        return [license!.ToLicenseRegistrationProperties()];
    }

    public ExplicitLicenseSource( string licenseString, LicenseSourceKind kind, IServiceProvider services )
        : base( services )
    {
        this._licenseString = licenseString;
        this.Kind = kind;
    }

    public override LicenseSourcePriority Priority => LicenseSourcePriority.Explicit;
}