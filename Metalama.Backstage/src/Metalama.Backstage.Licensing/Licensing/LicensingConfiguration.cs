// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Registration;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Serialization;

namespace Metalama.Backstage.Licensing;

[ConfigurationFile( "licensing.json" )]
internal sealed record LicensingConfiguration : ConfigurationFile
{
    /// <summary>
    /// Gets the date of the last evaluation period.
    /// </summary>
    [JsonPropertyName( "lastEvaluationStartDate" )]
    public DateTime? LastEvaluationStartDate { get; init; }

    /// <summary>
    /// Gets the only license key compatible with pre-2025.1, or <c>null</c> if there is none.
    /// </summary>
    [JsonPropertyName( "license" )]
    public string? LegacyLicense { get; init; }

    private readonly ImmutableArray<string?> _licenses = ImmutableArray<string?>.Empty;

    /// <summary>
    /// Gets the list of license keys for Metalama 2025.1 or later.
    /// </summary>
    /// <remarks>
    /// The value is normalized to <see cref="ImmutableArray{T}.Empty"/> because a default (uninitialized)
    /// <see cref="ImmutableArray{T}"/> wraps a null array and throws when it is enumerated or serialized. A property
    /// initializer alone is not enough: the System.Text.Json source generator treats every <c>init</c> property as a
    /// constructor parameter and assigns it unconditionally, so a <c>licensing.json</c> without a <c>licenses</c> entry
    /// (a fresh installation, or a file written by an earlier version) overwrites the initializer with the default value.
    /// </remarks>
    [JsonPropertyName( "licenses" )]
    public ImmutableArray<string?> Licenses
    {
        get => this._licenses;
        init => this._licenses = value.IsDefault ? ImmutableArray<string?>.Empty : value;
    }

    /// <summary>
    /// Gets the license keys that no released version of Metalama can consume, grouped by the minimal version of
    /// Metalama that can consume them. The key of the group is that version, formatted by <see cref="Version.ToString()"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Every installed version of Metalama reads the same <c>licensing.json</c>, so a license key that only a later
    /// version understands otherwise reaches the earlier versions, which report a message that the user cannot act
    /// upon, or throw. A version reads <see cref="LegacyLicense"/>, <see cref="Licenses"/> and the groups whose
    /// version is not greater than its own, and skips the other groups before their license keys are deserialized.
    /// </para>
    /// <para>
    /// The value is <c>null</c> when there is no group, and the member is then absent from the file, so registering
    /// a license key that every released version can consume writes the same file as before this member existed.
    /// </para>
    /// </remarks>
    [JsonPropertyName( "licensesByMinimalVersion" )]
    [JsonIgnore( Condition = JsonIgnoreCondition.WhenWritingNull )]
    public ImmutableDictionary<string, ImmutableArray<string?>>? LicensesByMinimalVersion { get; init; }

    public CommunityLicenseReason CommunityLicenseReason { get; init; }

    /// <summary>
    /// Registers a license key according to the rules of the catalog of PostSharp Technologies. This overload exists
    /// for compatibility; the overload that takes a catalog serves every product family.
    /// </summary>
    public LicensingConfiguration SetLicense( LicenseRegistrationProperties license )
        => this.SetLicense( license, PostSharpTechnologiesLicenseProductCatalog.Instance );

    /// <summary>
    /// Registers a license key, removing the keys of the products that do not co-exist with it, and storing the key
    /// where the versions that support its product read it.
    /// </summary>
    /// <param name="license">The properties of the license key.</param>
    /// <param name="catalog">The catalog that gives the registration rules of the products.</param>
    public LicensingConfiguration SetLicense( LicenseRegistrationProperties license, ILicenseProductCatalog catalog )
    {
        // First we should remove previous licenses except if they must co-exist for backward-compatibility reasons.
        var coexistingProducts = catalog.GetProductsCoexistingWith( license.Product );
        var clone = coexistingProducts.IsEmpty ? this.RemoveAllLicenses() : this.RemoveAllLicensesExcept( coexistingProducts );

        var licenseString = license.LicenseString ?? throw new ArgumentNullException( nameof(license) );

        // Now we can add the new license, in the oldest group that can consume it.
        if ( license.MinMetalamaVersion != null )
        {
            return clone with
            {
                LicensesByMinimalVersion = ImmutableDictionary<string, ImmutableArray<string?>>.Empty
                    .Add( license.MinMetalamaVersion.ToString(), ImmutableArray.Create<string?>( licenseString ) )
            };
        }
        else if ( catalog.RequiresVersionSpecificRegistration( license.Product ) )
        {
            return clone with { Licenses = ImmutableArray.Create<string?>( licenseString ) };
        }
        else
        {
            return clone with { LegacyLicense = licenseString };
        }
    }

    public LicensingConfiguration RemoveAllLicenses()
        => this with { LegacyLicense = null, Licenses = ImmutableArray<string?>.Empty, LicensesByMinimalVersion = null };

    private LicensingConfiguration RemoveAllLicensesExcept( ImmutableArray<LicenseProduct> products )
    {
        // A license key of a group is never a license key of a product that has to co-exist, because the products
        // that co-exist are consumed by every released version and therefore never reach a group.
        var clone = this.LicensesByMinimalVersion == null ? this : this with { LicensesByMinimalVersion = null };

        if ( clone.LegacyLicense != null
             && (GetLicenseKeyData( clone.LegacyLicense ) is not { } legacyLicense || !products.Contains( legacyLicense.Product )) )
        {
            return clone with { LegacyLicense = null };
        }
        else
        {
            return clone;
        }
    }

    private static LicenseKeyData? GetLicenseKeyData( string? licenseKey, Action<LicensingMessage>? reportMessage = null )
    {
        if ( string.IsNullOrWhiteSpace( licenseKey ) )
        {
            return null;
        }

        // ReSharper disable once RedundantSuppressNullableWarningExpression
        if ( !LicenseKeyData.TryDeserialize( licenseKey!, out var licenseKeyData, out var errorMessage ) )
        {
            reportMessage?.Invoke( new LicensingMessage( errorMessage ) );

            return null;
        }

        return licenseKeyData;
    }

    /// <summary>
    /// Enumerates the groups of <see cref="LicensesByMinimalVersion"/> whose name parses as a version and that carry
    /// at least one non-blank license key, ordered by that version. A group whose name does not parse as a version is
    /// skipped, as a later version may name a group in a way that the current version does not understand. A group
    /// whose license keys are all blank is skipped as well, so that it behaves as an empty group and is not reported
    /// as requiring a later version of Metalama.
    /// </summary>
    private IEnumerable<(Version MinimalVersion, ImmutableArray<string?> Licenses)> GetLicenseGroups()
    {
        if ( this.LicensesByMinimalVersion == null )
        {
            return [];
        }

        return this.LicensesByMinimalVersion
            .Where( group => !group.Value.IsDefaultOrEmpty && group.Value.Any( licenseKey => !string.IsNullOrWhiteSpace( licenseKey ) ) )
            .Select( group => (IsVersion: System.Version.TryParse( group.Key, out var version ), MinimalVersion: version, group.Value) )
            .Where( group => group.IsVersion )
            .Select( group => (MinimalVersion: group.MinimalVersion!, Licenses: group.Value) )
            .OrderBy( group => group.MinimalVersion );
    }

    /// <summary>
    /// Gets the minimal versions of the groups that <paramref name="currentVersion"/> does not support, ordered by
    /// version. The license keys of those groups are not deserialized, so the version of the group is the only
    /// information that the running version has about them.
    /// </summary>
    /// <param name="currentVersion">The version of the running product.</param>
    /// <returns>The minimal versions of the unsupported groups.</returns>
    public IEnumerable<Version> GetUnsupportedMinimalVersions( Version currentVersion )
        => this.GetLicenseGroups().Where( group => group.MinimalVersion > currentVersion ).Select( group => group.MinimalVersion );

    /// <summary>
    /// Gets all parsable license keys that the running version supports, regardless of whether they are valid or not.
    /// The license keys of a group whose version is greater than <paramref name="currentVersion"/> are not returned
    /// and are not deserialized, so they report no message.
    /// </summary>
    /// <param name="currentVersion">The version of the running product.</param>
    /// <param name="reportMessage">A delegate that receives the message reported by a license key that does not parse.</param>
    /// <returns>The license key data of the license keys that the running version supports.</returns>
    public IEnumerable<LicenseKeyData> GetRegisteredLicenses( Version currentVersion, Action<LicensingMessage>? reportMessage = null )
    {
        var licenses = new[] { this.LegacyLicense }
            .Concat( this.Licenses )
            .Concat( this.GetLicenseGroups().Where( group => group.MinimalVersion <= currentVersion ).SelectMany( group => group.Licenses ) );

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
