// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Registration
{
    public sealed class CommunityLicenseRegistrationTests : LicensingTestsBase
    {
        public CommunityLicenseRegistrationTests( ITestOutputHelper logger )
            : base( logger ) { }

        private void AssertSingleCommunityLicenseRegistered()
        {
            Assert.NotNull( this.LicenseRegistrationService.RegisteredLicense );
            Assert.False( string.IsNullOrEmpty( this.LicenseRegistrationService.RegisteredLicense.UniqueId ) );
            Assert.Equal( LicensedProduct.MetalamaCommunity, this.LicenseRegistrationService.RegisteredLicense.Product );
        }

        [Fact]
        public void RepeatedCommunityLicenseRegistrationKeepsSingleLicenseRegistered()
        {
            Assert.True( this.LicenseRegistrationService.TryRegisterCommunityEdition( out _ ) );
            Assert.True( this.LicenseRegistrationService.TryRegisterCommunityEdition( out _ ) );
            this.AssertSingleCommunityLicenseRegistered();

#pragma warning disable CA1307
            Assert.Single(
                this.Log.Entries,
                x => x.Message.Contains( "Failed to register Metalama Community: A Metalama Community license is registered already." ) );
#pragma warning restore CA1307
        }

        [Fact]
        public async Task NotifyPropertyChanged()
        {
            var gotPropertyChanged = new TaskCompletionSource<bool>();
            this.LicenseRegistrationService.PropertyChanged += ( _, _ ) => gotPropertyChanged.TrySetResult( true );

            Assert.True( this.LicenseRegistrationService.TryRegisterCommunityEdition( out _ ) );

            Assert.Equal( gotPropertyChanged.Task, await Task.WhenAny( gotPropertyChanged.Task, Task.Delay( 30000 ) ) );
        }
    }
}