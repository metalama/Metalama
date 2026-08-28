// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
            // The license string is typically supplied by a secret of a continuous integration server, so a mistyped
            // value is a likely mistake. The message names the source instead of quoting the value, which may be a
            // secret. The source provides no license, so the caller reports that no valid license was found.
            reportMessage( new LicensingMessage( $"The license key set in {this.Description} is invalid. {errorMessage}" ) );

            return [];
        }

        return [license.ToLicenseRegistrationProperties()];
    }

    public ExplicitLicenseSource( string licenseString, LicenseSourceKind kind, IServiceProvider services )
        : base( services )
    {
        this._licenseString = licenseString;
        this.Kind = kind;
    }

    public override LicenseSourcePriority Priority => LicenseSourcePriority.Explicit;
}
