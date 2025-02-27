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

    public CommunityLicenseReason CommunityLicenseReason { get; init; }

    public LicensingConfiguration SetLicense( LicenseRegistrationProperties license )
    {
        var clone = this;

        // First we should remove previous licenses except if they must co-exist for backward-compatibility reasons.
#pragma warning disable CS0618 // Type or member is obsolete
        if ( license.Product == LicenseProduct.MetalamaCommunity )
        {
            clone = clone.RemoveAllLicensesExcept( LicenseProduct.MetalamaFree );
        }
        else if ( license.Product == LicenseProduct.MetalamaFree )
        {
            clone = clone.RemoveAllLicensesExcept( LicenseProduct.MetalamaCommunity );
        }
#pragma warning restore CS0618 // Type or member is obsolete
        else
        {
            clone = clone.RemoveAllLicenses();
        }

        // Now we can add the new license.
        if ( !license.Product.IsSupportedBeforeMetalama20251() )
        {
            return clone with { Licenses = ImmutableArray.Create( license.LicenseString ?? throw new ArgumentNullException() )! };
        }
        else
        {
            return clone with { LegacyLicense = license.LicenseString };
        }
    }

    public LicensingConfiguration RemoveAllLicenses() => this with { LegacyLicense = null, Licenses = ImmutableArray<string?>.Empty };

    private LicensingConfiguration RemoveAllLicensesExcept( LicenseProduct product )
    {
        if ( this.LegacyLicense != null && GetLicenseKeyData( this.LegacyLicense )?.Product != product )
        {
            return this with { LegacyLicense = null };
        }
        else
        {
            return this;
        }
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