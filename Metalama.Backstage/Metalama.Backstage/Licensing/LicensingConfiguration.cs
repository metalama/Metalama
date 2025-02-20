// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Registration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Backstage.Licensing;

[ConfigurationFile( "licensing.json" )]
internal sealed record LicensingConfiguration : ConfigurationFile
{
    /// <summary>
    /// Gets the date of the last evaluation period.
    /// </summary>
    [JsonProperty( "lastEvaluationStartDate" )]
    public DateTime? LastEvaluationStartDate { get; init; }

    /// <summary>
    /// Gets the only license key compatible with pre-2025.1, or <c>null</c> if there is none.
    /// </summary>
    [JsonProperty( "license" )]
    public string? LegacyLicense { get; init; }

    /// <summary>
    /// Gets the list of license keys for Metalama 2025.1 or later.
    /// </summary>
    [JsonProperty( "licenses" )]
    public ImmutableArray<string?> Licenses { get; init; } = ImmutableArray<string?>.Empty;

    public LicensingConfiguration SetLicense( LicenseRegistrationProperties properties )
    {
        if ( properties.Product.IsSupportedBeforeMetalama20251() )
        {
            return this with { Licenses = ImmutableArray.Create( properties.LicenseString ?? throw new ArgumentNullException() )! };
        }
        else
        {
            return this with { LegacyLicense = properties.LicenseString };
        }
    }

    public LicensingConfiguration RemoveSupportedLicenses()
    {
        var clone = this;

        // Make sure we don't unregister license keys that we don't support.
        if ( GetLicenseKeyDataIfSupported( this.LegacyLicense ) != null )
        {
            clone = this with { LegacyLicense = null };
        }

        foreach ( var license in this.Licenses )
        {
            if ( GetLicenseKeyDataIfSupported( license ) != null )
            {
                clone = clone with { Licenses = clone.Licenses.Remove( license ) };
            }
        }

        return clone;
    }

    private static LicenseKeyData? GetLicenseKeyData( string? licenseKey, Action<LicensingMessage>? reportMessage = null )
    {
        if ( string.IsNullOrWhiteSpace( licenseKey ) )
        {
            return null;
        }

        if ( !LicenseKeyData.TryDeserialize( licenseKey!, out var licenseKeyData, out var errorMessage ) )
        {
            reportMessage?.Invoke( new LicensingMessage( errorMessage ) );

            return null;
        }

        return licenseKeyData;
    }

    private static LicenseKeyData? GetLicenseKeyDataIfSupported( string? licenseKey, Action<LicensingMessage>? reportMessage = null )
    {
        var licenseKeyData = GetLicenseKeyData( licenseKey, reportMessage );

        if ( licenseKeyData == null )
        {
            return null;
        }

        if ( !licenseKeyData.Product.IsSupportedSinceMetalama20251() )
        {
            return null;
        }

        return licenseKeyData;
    }

    /// <summary>
    /// Gets all parsable license keys, regardless of whether they are supported or not by the current version.
    /// </summary>
    public IEnumerable<LicenseKeyData> GetRegisteredLicenses( Action<LicensingMessage>? reportMessage = null )
    {
        var licenses = new[] { this.LegacyLicense }.Concat( this.Licenses );

        foreach ( var license in licenses )
        {
            var licenseKeyData = GetLicenseKeyData( license, reportMessage );

            if ( licenseKeyData != null )
            {
                yield return licenseKeyData;
            }
        }
    }
}