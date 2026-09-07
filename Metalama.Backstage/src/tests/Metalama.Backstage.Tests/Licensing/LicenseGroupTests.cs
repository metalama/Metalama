// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Licensing.Licenses;
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

    /// <summary>
    /// The minimal version of Metalama that a license key signed by the Elliptic Curve DSA authority of #1864
    /// requires, which is the version that introduces that authority.
    /// </summary>
    private static readonly Version _ecdsaMinimalVersion = new( 2027, 0 );

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
    /// before that authority cannot verify such a license key, so it is the first license key that requires a group.
    /// Its minimal version is detected from the identifier of the signature key.
    /// </summary>
    /// <returns>The license key.</returns>
    private static string CreateLicenseKeySignedByECDsaAuthority()
    {
        var builder = CreateLicenseKeyDataBuilder();

        Assert.True( builder.RequiresSignature() );

        return builder.SignAndSerialize( TestLicensingAuthorityProvider.ECDsaTestAuthority );
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
    /// Tests that registering a license key that the versions released before its licensing authority cannot consume
    /// places it in the group of its minimal compatible version, instead of the legacy property or the
    /// <c>licenses</c> array, which every released version reads.
    /// </summary>
    [Fact]
    public void RegisteringLicenseKeyOfNewAuthorityCreatesGroup()
    {
        var licenseKey = CreateLicenseKeySignedByECDsaAuthority();

        Assert.True( this.LicenseRegistrationService.RegisterLicense( licenseKey ).IsSuccess );

        var configuration = this.ConfigurationManager!.Get<LicensingConfiguration>();

        Assert.Null( configuration.LegacyLicense );
        Assert.DoesNotContain( licenseKey, configuration.Licenses );

        var group = Assert.Single( configuration.LicensesByMinimalVersion! );

        Assert.Equal( _ecdsaMinimalVersion.ToString(), group.Key );
        Assert.Equal( licenseKey, Assert.Single( group.Value ) );
    }

    /// <summary>
    /// Tests that a license key whose minimal version is greater than the version of the running product grants no
    /// license and reports no message, and that the version of its group is nevertheless reported, so that the user
    /// interface can tell the user which version of Metalama is required.
    /// </summary>
    /// <remarks>
    /// The group is written to the configuration instead of being registered, because the running version detects
    /// the minimal version of a license key from a format that it knows, and therefore cannot produce a license key
    /// that requires a version later than its own.
    /// </remarks>
    [Fact]
    public void LicenseKeyOfUnsupportedGroupGrantsNoLicenseAndReportsNoMessage()
    {
        this.SetLicensingConfiguration(
            $$"""
              {
                "licensesByMinimalVersion": { "{{_futureVersion}}": [ "{{_unparsableLicenseKey}}" ] }
              }
              """ );

        var (canConsume, messages) = this.TryConsumeFromUserProfile();

        Assert.False( canConsume );
        Assert.Empty( messages );
        Assert.Empty( this.LicenseRegistrationService.RegisteredLicenses );

        Assert.Equal(
            Version.Parse( _futureVersion ),
            Assert.Single( this.LicenseRegistrationService.UnsupportedRegisteredLicenseVersions ) );
    }

    /// <summary>
    /// Tests that the license key of the legacy property is consumed when the configuration also carries a group
    /// that the running version does not support, so that such a group never disables the license keys that the
    /// running version can consume.
    /// </summary>
    [Fact]
    public void UnsupportedGroupDoesNotPreventTheLegacyLicenseFromBeingConsumed()
    {
        this.SetLicensingConfiguration(
            $$"""
              {
                "license": "{{LicenseKeyProvider.MetalamaProfessionalBusiness}}",
                "licensesByMinimalVersion": { "{{_futureVersion}}": [ "{{_unparsableLicenseKey}}" ] }
              }
              """ );

        var (canConsume, messages) = this.TryConsumeFromUserProfile();

        Assert.True( canConsume );
        Assert.Empty( messages );

        Assert.Equal(
            LicenseKeyProvider.MetalamaProfessionalBusiness,
            this.LicenseRegistrationService.RegisteredLicenses.Single().LicenseString );
    }

    /// <summary>
    /// Tests that the content of <c>licensesByMinimalVersion</c> which the running version does not understand is
    /// skipped instead of throwing, that it reports no message, and that it does not prevent the license keys of the
    /// supported groups from being consumed.
    /// </summary>
    /// <remarks>
    /// The test covers a group whose name does not parse as a version, an empty group, and a group that carries a
    /// null license key.
    /// </remarks>
    [Fact]
    public void MalformedGroupsAreIgnored()
    {
        this.SetLicensingConfiguration(
            $$"""
              {
                "licensesByMinimalVersion": {
                  "not-a-version": [ "{{_unparsableLicenseKey}}" ],
                  "{{_futureVersion}}": [],
                  "{{_supportedVersion}}": [ null, "{{LicenseKeyProvider.MetalamaProfessionalBusiness}}" ]
                }
              }
              """ );

        var (canConsume, messages) = this.TryConsumeFromUserProfile();

        Assert.True( canConsume );
        Assert.Empty( messages );

        // Neither a group whose name does not parse as a version nor an empty group is reported to the user, because
        // the running version has nothing to tell about them.
        Assert.Empty( this.LicenseRegistrationService.UnsupportedRegisteredLicenseVersions );
    }

    /// <summary>
    /// Tests that registering a license key removes the license keys of every group, including the groups that the
    /// running version does not support, as it removes the license keys of the legacy properties.
    /// </summary>
    [Fact]
    public void RegisteringLicenseKeyRemovesTheUnsupportedGroups()
    {
        this.SetLicensingConfiguration(
            $$"""
              {
                "licensesByMinimalVersion": { "{{_futureVersion}}": [ "{{_unparsableLicenseKey}}" ] }
              }
              """ );

        Assert.True( this.LicenseRegistrationService.RegisterLicense( LicenseKeyProvider.MetalamaProfessionalBusiness ).IsSuccess );

        Assert.Empty( this.LicenseRegistrationService.UnsupportedRegisteredLicenseVersions );
        Assert.DoesNotContain( _unparsableLicenseKey, this.GetLicensingConfigurationJson(), StringComparison.Ordinal );
    }

    /// <summary>
    /// Tests that the minimal version of a license key is detected from the licensing authority that signs the
    /// license key, and that a license key which every version verifies requires no minimal version.
    /// </summary>
    /// <remarks>
    /// The detection is a property of <see cref="LicenseKeyData"/>, so it does not depend on a license field that
    /// the license generator would have to set.
    /// </remarks>
    [Fact]
    public void MinimalVersionIsDetectedFromTheLicensingAuthority()
    {
        var ecdsaLicenseKey = CreateLicenseKeySignedByECDsaAuthority();

        Assert.True( LicenseKeyData.TryDeserialize( ecdsaLicenseKey, out var ecdsaLicenseKeyData, out var errorMessage ), errorMessage );
        Assert.Equal( _ecdsaMinimalVersion, ecdsaLicenseKeyData.MinMetalamaVersion );
        Assert.True( ecdsaLicenseKeyData.ValidateFields( out errorMessage ), errorMessage );
        Assert.True( ecdsaLicenseKeyData.TryVerifySignature( this.LicensingAuthorityProvider, out errorMessage ), errorMessage );

        var dsaLicenseKey = CreateLicenseKeyDataBuilder().SignAndSerialize( LicenseKeyProvider.Authority );

        Assert.True( LicenseKeyData.TryDeserialize( dsaLicenseKey, out var dsaLicenseKeyData, out errorMessage ), errorMessage );
        Assert.Null( dsaLicenseKeyData.MinMetalamaVersion );
    }

    /// <summary>
    /// Tests that a group survives the round trip through the configuration file, so that a version which supports
    /// the group reads back what another version wrote.
    /// </summary>
    [Fact]
    public void GroupSurvivesTheRoundTripThroughTheConfigurationFile()
    {
        var licenseKey = CreateLicenseKeySignedByECDsaAuthority();

        Assert.True( this.LicenseRegistrationService.RegisterLicense( licenseKey ).IsSuccess );

        var json = this.GetLicensingConfigurationJson();

        Assert.True( this.JsonSerializationService.TryDeserialize<LicensingConfiguration>( json, out var configuration ) );

        var group = Assert.Single( configuration!.LicensesByMinimalVersion! );

        Assert.Equal( _ecdsaMinimalVersion.ToString(), group.Key );
        Assert.Equal( licenseKey, Assert.Single( group.Value ) );
    }
}
