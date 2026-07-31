// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing;
using Metalama.Backstage.Serialization;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing
{
    /// <summary>
    /// Regression tests for the <c>default</c> (uninitialized) <see cref="System.Collections.Immutable.ImmutableArray{T}"/>
    /// that <see cref="LicensingConfiguration.Licenses"/> could hold.
    /// </summary>
    /// <remarks>
    /// See issue #1766. A <c>default(ImmutableArray&lt;T&gt;)</c> wraps a null backing array: it is not <c>Empty</c>, and
    /// touching it throws. Three <c>metalama license</c> commands crashed on it: <c>list</c> and <c>register-trial</c> with
    /// an <see cref="InvalidOperationException"/> from <see cref="LicensingConfiguration.GetRegisteredLicenses"/>, and
    /// <c>unregister</c> with a <see cref="NullReferenceException"/> from the source-generated
    /// <c>ImmutableArrayStringSerializeHandler</c>, reached from <c>ConfigurationManager.StructurallyEquals</c>.
    /// </remarks>
    public sealed class LicensingConfigurationTests : LicensingTestsBase
    {
        public LicensingConfigurationTests( ITestOutputHelper logger )
            : base( logger ) { }

        private Metalama.Backstage.Configuration.ConfigurationManager CreateConfigurationManager()
            => new( this.ServiceProvider );

        [Fact]
        public void GetRegisteredLicenses_WithDefaultLicenses_DoesNotThrow()
        {
            // This is the 'license list' and 'license register-trial' crash: both reach GetRegisteredLicenses, which
            // concatenates the Licenses array and therefore throws InvalidOperationException on a default instance.
            var configuration = new LicensingConfiguration { Licenses = default };

            var licenses = configuration.GetRegisteredLicenses( message => this.Logger.WriteLine( message.ToString()! ) ).ToList();

            Assert.Empty( licenses );
        }

        [Fact]
        public void GetRegisteredLicenses_WithDefaultLicensesAndLegacyLicense_ReturnsLegacyLicense()
        {
            // The legacy license is concatenated with the Licenses array, so it must still be returned when the array
            // is a default instance.
            var configuration = new LicensingConfiguration { Licenses = default, LegacyLicense = LicenseKeyProvider.PostSharpFramework };

            var licenses = configuration.GetRegisteredLicenses( message => this.Logger.WriteLine( message.ToString()! ) ).ToList();

            Assert.Single( licenses );
        }

        [Fact]
        public void RegisteredLicenses_WithDefaultLicenses_IsEmpty()
        {
            // The same crash as above, but through the service the 'license list' command actually uses.
            this.ConfigurationManager!.Update<LicensingConfiguration>( c => c with { Licenses = default } );

            Assert.Empty( this.LicenseRegistrationService.RegisteredLicenses );
        }

        [Fact]
        public void RegisterTrialEdition_WithDefaultLicenses_Succeeds()
        {
            // The 'license register-trial' crash: CanRegisterTrialEditionCore enumerates GetRegisteredLicenses.
            this.ConfigurationManager!.Update<LicensingConfiguration>( c => c with { Licenses = default } );

            Assert.True( this.LicenseRegistrationService.RegisterTrialEdition().IsSuccess );
            Assert.Single( this.LicenseRegistrationService.RegisteredLicenses );
        }

        [Fact]
        public void RemoveLicenses_WithDefaultLicenses_DoesNotThrow()
        {
            // The 'license unregister' crash: RemoveLicenses updates the configuration file, and the update path
            // serializes the configuration to compare it structurally with the cached one.
            this.ConfigurationManager!.Update<LicensingConfiguration>( c => c with { Licenses = default } );

            this.LicenseRegistrationService.RemoveLicenses();

            Assert.Empty( this.LicenseRegistrationService.RegisteredLicenses );
        }

        [Fact]
        public void Serialize_WithDefaultLicenses_DoesNotThrow()
        {
            // The source-generated ImmutableArrayStringSerializeHandler dereferences the backing array without checking
            // IsDefault, so it throws a bare NullReferenceException instead of the framework's explicit message.
            var jsonSerializationService = this.ServiceProvider.GetRequiredBackstageService<IJsonSerializationService>();

            var json = jsonSerializationService.Serialize( new LicensingConfiguration { Licenses = default }, typeof(LicensingConfiguration) );

            this.Logger.WriteLine( json );
            Assert.Contains( "\"licenses\": []", json, StringComparison.Ordinal );
        }

        [Theory]
        [InlineData( "{}" )]
        [InlineData( """{ "licenses": null }""" )]
        [InlineData( """{ "license": "SOME-LEGACY-KEY" }""" )]
        public void ReadConfigurationFile_WithoutLicenses_YieldsEmptyArray( string json )
        {
            // A licensing.json written by an older version, or one the user edited with 'metalama config edit licensing',
            // simply has no 'licenses' entry. Deserializing it must yield an empty array rather than a default one.
            var configurationManager = this.CreateConfigurationManager();
            var filePath = configurationManager.GetFilePath<LicensingConfiguration>();
            this.FileSystem.WriteAllText( filePath, json );

            var configuration = configurationManager.Get<LicensingConfiguration>();

            Assert.False( configuration.Licenses.IsDefault );
            Assert.Empty( configuration.Licenses );

            // Updating the file goes through ConfigurationManager.StructurallyEquals, which serializes both the cached
            // and the new instance. This is where the NullReferenceException of the 'license unregister' report came from.
            configurationManager.Update<LicensingConfiguration>( c => c with { LastEvaluationStartDate = this.Time.UtcNow } );

            Assert.False( configurationManager.Get<LicensingConfiguration>().Licenses.IsDefault );
        }
    }
}
