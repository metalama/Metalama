// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Registration;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Registration
{
    public class LicenseFileStorageTests : LicensingTestsBase
    {
        public LicenseFileStorageTests( ITestOutputHelper logger )
            : base( logger ) { }

        protected override void ConfigureServices( ServiceProviderBuilder services )
        {
            base.ConfigureServices( services );
            services.AddSingleton<IConfigurationManager>( serviceProvider => new Configuration.ConfigurationManager( serviceProvider ) );
        }

        private LicensingConfigurationModel OpenOrCreateStorage()
        {
            return LicensingConfigurationModel.Create( this.ServiceProvider );
        }

        private void AssertFileContains( string? expectedLicenseString )
        {
            Assert.Equal( expectedLicenseString, this.ReadStoredLicenseString() );
        }

        private void AssertStorageContains( LicensingConfigurationModel storage, string? expectedLicenseString )
        {
            Assert.Equal( expectedLicenseString, storage.LicenseString );

            if ( expectedLicenseString == null )
            {
                Assert.Null( storage.LicenseProperties );
            }
            else
            {
                if ( !this.LicenseFactory.TryCreate( expectedLicenseString, out var expectedLicense, out _ ) )
                {
                    Assert.Null( storage.LicenseProperties );

                    return;
                }

                if ( !expectedLicense.TryGetProperties( out var expectedData, out _ ) )
                {
                    Assert.Null( storage.LicenseProperties );

                    return;
                }

                Assert.Equal( expectedData, storage.LicenseProperties );
            }
        }

        private void Add( LicensingConfigurationModel storage, string licenseString )
        {
            var data = this.GetLicenseRegistrationData( licenseString );
            storage.SetLicense( licenseString, data );
        }

        [Fact]
        public void ExistingStorageSucceedsToRead()
        {
            this.SetStoredLicenseString( "dummy" );
            this.AssertFileContains( "dummy" );
        }

        [Fact]
        public void NonExistentStorageCanBeRead()
        {
            var storage = this.OpenOrCreateStorage();
            this.AssertStorageContains( storage, null );
        }

        [Fact]
        public void EmptyStorageCanBeCreated()
        {
            this.OpenOrCreateStorage();
            this.AssertFileContains( null );
        }

        [Fact]
        public void NonEmptyStorageCanBeCreated()
        {
            var storage = this.OpenOrCreateStorage();
            this.Add( storage, LicenseKeyProvider.PostSharpUltimate );

            this.AssertFileContains( LicenseKeyProvider.PostSharpUltimate );
        }

        [Fact]
        public void ValidLicenseKeyCanBeRetrieved()
        {
            this.SetStoredLicenseString( LicenseKeyProvider.PostSharpUltimate );

            var storage = this.OpenOrCreateStorage();

            this.AssertStorageContains( storage, LicenseKeyProvider.PostSharpUltimate );
        }

        [Fact]
        public void InvalidLicenseKeyCanBeRetrieved()
        {
            this.SetStoredLicenseString( "dummy" );

            var storage = this.OpenOrCreateStorage();

            this.AssertStorageContains( storage, "dummy" );
            this.AssertFileContains( "dummy" );
        }

        [Fact]
        public void PreviousInvalidLicenseKeyIsReplaced()
        {
            this.SetStoredLicenseString( "dummy" );

            var storage = this.OpenOrCreateStorage();
            this.Add( storage, LicenseKeyProvider.PostSharpUltimate );

            this.AssertStorageContains( storage, LicenseKeyProvider.PostSharpUltimate );
            this.AssertFileContains( LicenseKeyProvider.PostSharpUltimate );
        }

        [Fact]
        public void PreviousValidLicenseKeysAreReplaced()
        {
            this.SetStoredLicenseString( LicenseKeyProvider.PostSharpUltimate );

            var storage = this.OpenOrCreateStorage();
            this.Add( storage, LicenseKeyProvider.MetalamaStarterPersonal );

            this.AssertStorageContains( storage, LicenseKeyProvider.MetalamaStarterPersonal );
            this.AssertFileContains( LicenseKeyProvider.MetalamaStarterPersonal );
        }
    }
}