// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Licensing.Licenses.LicenseFields;
using Metalama.Backstage.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing;

/// <summary>
/// Tests that the registered license keys are grouped by the minimal version of Metalama that can consume them, so
/// that a version reads only the groups it supports. Every installed version reads the same <c>licensing.json</c>,
/// so a license key that only a later version understands otherwise reaches the earlier versions, which report a
/// message that the user cannot act upon, or throw. See issue #1922.
/// </summary>
public sealed class LicenseGroupTests : LicensingTestsBase
{
    /// <summary>
    /// The name of the group that carries the license keys which the running version supports. It is not greater
    /// than the version of the test application, which is 1.0.
    /// </summary>
    private const string _supportedVersion = "1.0";

    /// <summary>
    /// The name of the group that carries the license keys which no released version supports.
    /// </summary>
    private const string _futureVersion = "9999.0";

    /// <summary>
    /// A license key of a format that the running version cannot parse. It stands for a license key that only a
    /// later version can consume. A test that places it in an unsupported group therefore proves that the group is
    /// skipped before its license keys are deserialized.
    /// </summary>
    private const string _unparsableLicenseKey = "999-THIS-LICENSE-KEY-REQUIRES-A-LATER-VERSION";

    public LicenseGroupTests( ITestOutputHelper logger ) : base( logger ) { }

    private IJsonSerializationService JsonSerializationService
        => this.ServiceProvider.GetRequiredBackstageService<IJsonSerializationService>();

    /// <summary>
    /// Replaces the licensing configuration by the one that a JSON document represents, as if the user profile
    /// carried that document.
    /// </summary>
    /// <param name="json">The content of <c>licensing.json</c>.</param>
    private void SetLicensingConfiguration( string json )
    {
        Assert.True( this.JsonSerializationService.TryDeserialize<LicensingConfiguration>( json, out var configuration ) );

        this.ConfigurationManager!.Set( configuration! );
    }

    /// <summary>
    /// Returns the JSON representation of the current licensing configuration.
    /// </summary>
    /// <returns>The content of <c>licensing.json</c>.</returns>
    private string GetLicensingConfigurationJson()
    {
        var json = this.JsonSerializationService.Serialize(
            this.ConfigurationManager!.Get<LicensingConfiguration>(),
            typeof(LicensingConfiguration) );

        this.Logger.WriteLine( json );

        return json;
    }

    /// <summary>
    /// Reads the license keys of the user profile, which is the path that a compilation takes.
    /// </summary>
    /// <returns>The license keys that the running version consumes, and the messages that reading them reported.</returns>
    private (List<ILicense> Licenses, List<LicensingMessage> Messages) GetLicensesFromUserProfile()
    {
        var messages = new List<LicensingMessage>();
        var source = new UserProfileLicenseSource( this.ServiceProvider );
        var licenses = source.GetLicenses( messages.Add ).ToList();

        foreach ( var message in messages )
        {
            this.Logger.WriteLine( message.ToString()! );
        }

        return (licenses, messages);
    }

    /// <summary>
    /// Creates a license key that is signed by the Elliptic Curve DSA authority of #1864. The versions released
    /// before that authority cannot verify such a key, so it is the first license key that requires a group. The
    /// license key carries no minimal version, so the minimal version comes from the rule that is based on the
    /// identifier of the signature key.
    /// </summary>
    /// <returns>The license key.</returns>
    private static string CreateLicenseKeyRequiringLaterVersion()
    {
        var builder = CreateLicenseKeyDataBuilder();

        Assert.True( builder.RequiresSignature() );

        return builder.SignAndSerialize( TestLicensingAuthorityProvider.ECDsaTestAuthority );
    }

    /// <summary>
    /// Creates a license key that carries a minimal version and that is signed by the finite field DSA authority,
    /// which every version verifies. The minimal version therefore comes from the license key itself.
    /// </summary>
    /// <param name="minMetalamaVersion">The minimal version of Metalama that can consume the license key.</param>
    /// <returns>The license key.</returns>
    private static string CreateLicenseKeyRequiringVersion( Version minMetalamaVersion )
    {
        var builder = CreateLicenseKeyDataBuilder();
        builder.MinMetalamaVersion = minMetalamaVersion;

        return builder.SignAndSerialize( LicenseKeyProvider.Authority );
    }

    private static LicenseKeyDataBuilder CreateLicenseKeyDataBuilder()
        => new()
        {
            LicenseId = 801,
            Product = LicenseProduct.MetalamaProfessional,
            LicenseType = LicenseType.Business,
            Generation = LicenseGeneration.Current,
            SubscriptionEndDate = LicenseKeyProvider.DefaultSubscriptionExpirationDate
        };

    /// <summary>
    /// Attempts to consume a license from the user profile.
    /// </summary>
    /// <returns>Whether a license was granted, and the messages that the consumption reported.</returns>
    private (bool CanConsume, List<LicensingMessage> Messages) TryConsumeFromUserProfile()
    {
        var messages = new List<LicensingMessage>();
        var service = new LicenseConsumptionService( this.ServiceProvider, [new UserProfileLicenseSource( this.ServiceProvider )] );
        var canConsume = service.CreateConsumer( LicenseConsumptionOptions.Default, messages.Add ).TryConsume( LicenseRequirement.Any );

        foreach ( var message in messages )
        {
            this.Logger.WriteLine( message.ToString()! );
        }

        return (canConsume, messages);
    }

    /// <summary>
    /// Tests that a license key of a group whose version is not greater than the version of the running product is
    /// consumed exactly as a license key of the <c>licenses</c> array is consumed.
    /// </summary>
    [Fact]
    public void LicenseKeyOfSupportedGroupIsConsumed()
    {
        this.SetLicensingConfiguration(
            $$"""
              {
                "licensesByMinimalVersion": { "{{_supportedVersion}}": [ "{{LicenseKeyProvider.MetalamaProfessionalBusiness}}" ] }
              }
              """ );

        var (licenses, messages) = this.GetLicensesFromUserProfile();

        Assert.Single( licenses );
        Assert.Empty( messages );

        Assert.Equal(
            LicenseKeyProvider.MetalamaProfessionalBusiness,
            this.LicenseRegistrationService.RegisteredLicenses.Single().LicenseString );
    }

    /// <summary>
    /// Tests that a license key of a group whose version is greater than the version of the running product is not
    /// deserialized, reports no message and throws no exception.
    /// </summary>
    [Fact]
    public void LicenseKeyOfUnsupportedGroupIsIgnored()
    {
        this.SetLicensingConfiguration(
            $$"""
              {
                "licensesByMinimalVersion": { "{{_futureVersion}}": [ "{{_unparsableLicenseKey}}" ] }
              }
              """ );

        var (licenses, messages) = this.GetLicensesFromUserProfile();

        Assert.Empty( licenses );
        Assert.Empty( messages );
        Assert.Empty( this.LicenseRegistrationService.RegisteredLicenses );
    }

    /// <summary>
    /// Tests that the license keys of the legacy property, of the <c>licenses</c> array and of a supported group
    /// are read together, and that an unsupported group beside them changes nothing.
    /// </summary>
    [Fact]
    public void SupportedGroupsAreReadBesideTheLegacyProperties()
    {
        this.SetLicensingConfiguration(
            $$"""
              {
                "license": "{{LicenseKeyProvider.PostSharpFramework}}",
                "licenses": [ "{{LicenseKeyProvider.MetalamaCommunity}}" ],
                "licensesByMinimalVersion": {
                  "{{_supportedVersion}}": [ "{{LicenseKeyProvider.MetalamaProfessionalBusiness}}" ],
                  "{{_futureVersion}}": [ "{{_unparsableLicenseKey}}" ]
                }
              }
              """ );

        var (licenses, messages) = this.GetLicensesFromUserProfile();

        Assert.Equal( 3, licenses.Count );
        Assert.Empty( messages );

        var registeredLicenseStrings = this.LicenseRegistrationService.RegisteredLicenses.Select( l => l.LicenseString ).ToList();

        Assert.Contains( LicenseKeyProvider.PostSharpFramework, registeredLicenseStrings );
        Assert.Contains( LicenseKeyProvider.MetalamaCommunity, registeredLicenseStrings );
        Assert.Contains( LicenseKeyProvider.MetalamaProfessionalBusiness, registeredLicenseStrings );
    }

    /// <summary>
    /// Tests that the licensing services initialize when the configuration carries a group of a version that the
    /// running product does not know, and that the license keys of the supported groups are still consumed.
    /// </summary>
    [Fact]
    public void UnknownFutureGroupDoesNotPreventConsumption()
    {
        this.SetLicensingConfiguration(
            $$"""
              {
                "licenses": [ "{{LicenseKeyProvider.MetalamaProfessionalBusiness}}" ],
                "licensesByMinimalVersion": { "{{_futureVersion}}": [ "{{_unparsableLicenseKey}}" ] }
              }
              """ );

        this.EnsureServicesInitialized();

        var (licenses, messages) = this.GetLicensesFromUserProfile();

        Assert.Single( licenses );
        Assert.Empty( messages );
    }

    /// <summary>
    /// Tests that removing the registered license keys clears every group, including the groups that the running
    /// version does not support.
    /// </summary>
    [Fact]
    public void RemoveLicensesClearsEveryGroup()
    {
        this.SetLicensingConfiguration(
            $$"""
              {
                "licenses": [ "{{LicenseKeyProvider.MetalamaCommunity}}" ],
                "licensesByMinimalVersion": {
                  "{{_supportedVersion}}": [ "{{LicenseKeyProvider.MetalamaProfessionalBusiness}}" ],
                  "{{_futureVersion}}": [ "{{_unparsableLicenseKey}}" ]
                }
              }
              """ );

        this.LicenseRegistrationService.RemoveLicenses();

        Assert.Empty( this.LicenseRegistrationService.RegisteredLicenses );

        var json = this.GetLicensingConfigurationJson();

        Assert.DoesNotContain( LicenseKeyProvider.MetalamaCommunity, json, StringComparison.Ordinal );
        Assert.DoesNotContain( LicenseKeyProvider.MetalamaProfessionalBusiness, json, StringComparison.Ordinal );
        Assert.DoesNotContain( _unparsableLicenseKey, json, StringComparison.Ordinal );
    }

    /// <summary>
    /// Tests that registering a license key that every supported version can consume produces the same configuration
    /// as it produces today, which means the legacy property and no group.
    /// </summary>
    [Fact]
    public void RegisteringOrdinaryLicenseKeyCreatesNoGroup()
    {
        Assert.True( this.LicenseRegistrationService.RegisterLicense( LicenseKeyProvider.MetalamaProfessionalBusiness ).IsSuccess );

        var configuration = this.ConfigurationManager!.Get<LicensingConfiguration>();

        Assert.Equal( LicenseKeyProvider.MetalamaProfessionalBusiness, configuration.LegacyLicense );

        var json = this.GetLicensingConfigurationJson();

        Assert.DoesNotContain( "licensesByMinimalVersion", json, StringComparison.Ordinal );
    }

    /// <summary>
    /// Tests that registering a license key that the released versions cannot consume places it in a group named
    /// after its minimal compatible version, instead of the legacy property or the <c>licenses</c> array.
    /// </summary>
    [Fact]
    public void RegisteringLicenseKeyRequiringLaterVersionCreatesGroup()
    {
        var licenseKey = CreateLicenseKeyRequiringLaterVersion();

        Assert.True( this.LicenseRegistrationService.RegisterLicense( licenseKey ).IsSuccess );

        var configuration = this.ConfigurationManager!.Get<LicensingConfiguration>();

        Assert.Null( configuration.LegacyLicense );
        Assert.DoesNotContain( licenseKey, configuration.Licenses );

        var json = this.GetLicensingConfigurationJson();

        Assert.Contains( "licensesByMinimalVersion", json, StringComparison.Ordinal );
        Assert.Contains( licenseKey, json, StringComparison.Ordinal );
    }

    /// <summary>
    /// Tests that registering a license key whose minimal version is greater than the version of the test
    /// application grants no license and reports no message, and that the license key is nevertheless stored and
    /// reported as requiring a later version.
    /// </summary>
    [Fact]
    public void LicenseKeyRequiringFutureVersionGrantsNoLicenseAndReportsNoMessage()
    {
        var futureVersion = new Version( 9999, 0 );
        var licenseKey = CreateLicenseKeyRequiringVersion( futureVersion );

        Assert.True( this.LicenseRegistrationService.RegisterLicense( licenseKey ).IsSuccess );

        var (canConsume, messages) = this.TryConsumeFromUserProfile();

        Assert.False( canConsume );
        Assert.Empty( messages );
        Assert.Empty( this.LicenseRegistrationService.RegisteredLicenses );

        // The license key is stored, and the version of its group is reported, so that the user interface can tell
        // the user which version of Metalama is required.
        Assert.Equal( futureVersion, Assert.Single( this.LicenseRegistrationService.UnsupportedRegisteredLicenseVersions ) );
        Assert.Contains( licenseKey, this.GetLicensingConfigurationJson(), StringComparison.Ordinal );
    }

    /// <summary>
    /// Tests that the minimal version carried by the license key takes precedence over the rule that is based on the
    /// properties of the license key. The license key of this test is signed by the Elliptic Curve DSA authority,
    /// for which the rule gives 2027.0, and carries a minimal version of 1.0.
    /// </summary>
    [Fact]
    public void MinimalVersionOfTheLicenseKeyTakesPrecedenceOverTheRule()
    {
        var builder = CreateLicenseKeyDataBuilder();
        builder.MinMetalamaVersion = new Version( 1, 0 );
        var licenseKey = builder.SignAndSerialize( TestLicensingAuthorityProvider.ECDsaTestAuthority );

        Assert.True( this.LicenseRegistrationService.RegisterLicense( licenseKey ).IsSuccess );

        var (licenses, messages) = this.GetLicensesFromUserProfile();

        Assert.Single( licenses );
        Assert.Empty( messages );
        Assert.Empty( this.LicenseRegistrationService.UnsupportedRegisteredLicenseVersions );
    }

    /// <summary>
    /// Tests that the minimal version is carried by an optional license field that is preceded by its length, so
    /// that a version of Metalama which does not declare the field ignores it instead of rejecting the license key.
    /// </summary>
    [Fact]
    public void MinimalVersionIsCarriedByAnOptionalLicenseField()
    {
        Assert.False( LicenseFieldIndex.MinMetalamaVersion.IsMustUnderstand() );
        Assert.True( LicenseFieldIndex.MinMetalamaVersion.IsPrefixedByLength() );

        var licenseKey = CreateLicenseKeyRequiringVersion( new Version( 2027, 0 ) );

        Assert.True( LicenseKeyData.TryDeserialize( licenseKey, out var licenseKeyData, out var errorMessage ), errorMessage );
        Assert.Equal( new Version( 2027, 0 ), licenseKeyData.MinMetalamaVersion );
        Assert.True( licenseKeyData.ValidateFields( out errorMessage ), errorMessage );
        Assert.True( licenseKeyData.TryVerifySignature( this.LicensingAuthorityProvider, out errorMessage ), errorMessage );
    }

    /// <summary>
    /// Tests that a group survives the round trip through the configuration file, so that a version which supports
    /// the group reads back what another version wrote.
    /// </summary>
    [Fact]
    public void GroupSurvivesTheRoundTripThroughTheConfigurationFile()
    {
        var licenseKey = CreateLicenseKeyRequiringVersion( new Version( 9999, 0 ) );

        Assert.True( this.LicenseRegistrationService.RegisterLicense( licenseKey ).IsSuccess );

        var json = this.GetLicensingConfigurationJson();

        Assert.True( this.JsonSerializationService.TryDeserialize<LicensingConfiguration>( json, out var configuration ) );

        var group = Assert.Single( configuration!.LicensesByMinimalVersion! );

        Assert.Equal( "9999.0", group.Key );
        Assert.Equal( licenseKey, Assert.Single( group.Value ) );
    }
}
