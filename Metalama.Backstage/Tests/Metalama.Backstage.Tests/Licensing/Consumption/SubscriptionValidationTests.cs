// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Testing;
using System;
using System.Collections.Immutable;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption
{
    public class SubscriptionValidationTests : LicensingTestsBase
    {
        public SubscriptionValidationTests( ITestOutputHelper logger )
            : base( logger ) { }

        private static IApplicationInfo CreateApplicationInfo( DateTime buildDate, params IComponentInfo[] components )
            => new TestApplicationInfo(
                $"Subscription Validation Test App built {buildDate:d}",
                false,
                $"<ver-{buildDate:d}>",
                buildDate ) { Components = components.ToImmutableArray() };

        private static IComponentInfo CreateComponentInfo( DateTime buildDate, bool isThirdParty )
            => new TestComponentInfo(
                $"Subscription Validation Test Component built {buildDate:d} {(isThirdParty ? "not by us" : "by us")}",
                $"<ver-{buildDate:d}>",
                false,
                buildDate,
                isThirdParty ? "The Corp" : "PostSharp Technologies" );

        private void Test( IApplicationInfo applicationInfo, IComponentInfo? infringingComponent = null )
        {
            var licenseKey = LicenseKeyProvider.MetalamaUltimateBusiness;

            var isDeserialized = LicenseKeyData.TryDeserialize( licenseKey, out var data, out _ );

            Assert.True( isDeserialized );

            var isValid = data!.Validate( null, this.Time, applicationInfo, out var actualErrorDescription );

            if ( infringingComponent == null )
            {
                Assert.True( isValid );
                Assert.Null( actualErrorDescription );
            }
            else
            {
                Assert.False( isValid );
#pragma warning disable CA1307 // Specify StringComparison for clarity
                Assert.Contains( infringingComponent.Name, actualErrorDescription! );
#pragma warning restore CA1307 // Specify StringComparison for clarity
            }
        }

        [Fact]
        public void PassesWithValidSubscriptionForApplicationInfo()
        {
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate );
            this.Test( applicationInfo );
        }

        [Fact]
        public void FailsWithInvalidSubscriptionForApplicationInfo()
        {
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate.AddDays( 1 ) );
            this.Test( applicationInfo, applicationInfo );
        }

        [Fact]
        public void PassesWithValidSubscriptionForComponentRequiringSubscription()
        {
            var componentInfo = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate, false );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate, componentInfo );
            this.Test( applicationInfo );
        }

        [Fact]
        public void FailsWithInvalidSubscriptionForComponentRequiringSubscription()
        {
            var componentInfo = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate.AddDays( 1 ), false );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate, componentInfo );
            this.Test( applicationInfo, componentInfo );
        }

        [Fact]
        public void PassesWithInvalidSubscriptionForComponentNotRequiringSubscription()
        {
            var componentInfo = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate.AddDays( 1 ), true );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate, componentInfo );
            this.Test( applicationInfo );
        }

        [Fact]
        public void FailsWithMultipleComponentsAndValidApplication()
        {
            var componentInfo1 = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate.AddDays( 1 ), false );
            var componentInfo2 = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate, false );
            var componentInfo3 = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate.AddDays( 1 ), true );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate, componentInfo1, componentInfo2, componentInfo3 );
            this.Test( applicationInfo, componentInfo1 );
        }

        [Fact]
        public void FailsWithMultipleComponentsAndInvalidApplication()
        {
            var componentInfo1 = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate, false );
            var componentInfo2 = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate.AddDays( 1 ), true );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate.AddDays( 1 ), componentInfo1, componentInfo2 );
            this.Test( applicationInfo, applicationInfo );
        }
    }
}