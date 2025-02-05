// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing;
using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Essentials
{
    public class FreeLicenseRegistrationTests : LicensingTestsBase
    {
        public FreeLicenseRegistrationTests( ITestOutputHelper logger )
            : base( logger ) { }

        private void AssertSingleFreeLicenseRegistered()
        {
            var registeredLicenseString = this.ReadStoredLicenseString();
            Assert.True( this.LicenseFactory.TryCreate( registeredLicenseString, out var registeredLicense, out var errorMessage ) );
            Assert.Null( errorMessage );
            Assert.True( registeredLicense.TryGetProperties( out var data, out errorMessage ) );
            Assert.Null( errorMessage );
            Assert.True( Guid.TryParse( data.UniqueId, out var id ) );
            Assert.NotEqual( Guid.Empty, id );
            Assert.Equal( LicensedProduct.MetalamaFree, data.Product );
        }

        [Fact]
        public void FreeLicenseRegistersInCleanEnvironment()
        {
            Assert.True( this.LicenseRegistrationService.TryRegisterFreeEdition( out _ ) );
            this.AssertSingleFreeLicenseRegistered();
        }

        [Fact]
        public void RepeatedFreeLicenseRegistrationKeepsSingleLicenseRegistered()
        {
            Assert.True( this.LicenseRegistrationService.TryRegisterFreeEdition( out _ ) );
            Assert.True( this.LicenseRegistrationService.TryRegisterFreeEdition( out _ ) );
            this.AssertSingleFreeLicenseRegistered();

#pragma warning disable CA1307
            Assert.Single(
                this.Log.Entries,
                x => x.Message.Contains( "Failed to register Metalama Free license: A Metalama Free license is registered already." ) );
#pragma warning restore CA1307
        }

        [Fact]
        public async Task NotifyPropertyChanged()
        {
            var gotPropertyChanged = new TaskCompletionSource<bool>();
            this.LicenseRegistrationService.PropertyChanged += ( _, _ ) => gotPropertyChanged.TrySetResult( true );

            Assert.True( this.LicenseRegistrationService.TryRegisterFreeEdition( out _ ) );

            Assert.Equal( gotPropertyChanged.Task, await Task.WhenAny( gotPropertyChanged.Task, Task.Delay( 30000 ) ) );
        }
    }
}