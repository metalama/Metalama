// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing;
using System.Linq;
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
            Assert.Single( this.LicenseRegistrationService.RegisteredLicenses );
            Assert.False( string.IsNullOrEmpty( this.LicenseRegistrationService.RegisteredLicenses.Single().UniqueId ) );
            Assert.Equal( LicenseProduct.MetalamaCommunity, this.LicenseRegistrationService.RegisteredLicenses.Single().Product );
        }

        [Fact]
        public void RepeatedCommunityLicenseRegistrationKeepsSingleLicenseRegistered()
        {
            Assert.True( this.LicenseRegistrationService.RegisterCommunityEdition( CommunityLicenseReason.Individual ).IsSuccess );
            Assert.True( this.LicenseRegistrationService.RegisterCommunityEdition( CommunityLicenseReason.Individual ).IsSuccess );
            this.AssertSingleCommunityLicenseRegistered();
        }

        [Fact]
        public async Task NotifyPropertyChanged()
        {
            var gotPropertyChanged = new TaskCompletionSource<bool>();
            this.LicenseRegistrationService.PropertyChanged += ( _, _ ) => gotPropertyChanged.TrySetResult( true );

            Assert.True( this.LicenseRegistrationService.RegisterCommunityEdition( CommunityLicenseReason.Individual ).IsSuccess );

            Assert.Equal( gotPropertyChanged.Task, await Task.WhenAny( gotPropertyChanged.Task, Task.Delay( 30000 ) ) );
        }
    }
}