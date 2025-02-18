// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Immutable;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption
{
    public sealed class SubscriptionValidationTests : LicensingTestsBase
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
            var licenseKey = LicenseKeyProvider.MetalamaProfessionalBusiness;
            var serviceCollection = this.CloneServiceCollection();
            serviceCollection.AddSingleton<IApplicationInfoProvider>( new ApplicationInfoProvider( applicationInfo ) );
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var licenseConsumer = LicenseConsumer.Create(
                LicenseConsumptionOptions.Default,
                serviceProvider,
                [new ExplicitLicenseSource( licenseKey, serviceProvider )] );

            var canConsume = licenseConsumer.TryConsume( _ => true );

            if ( infringingComponent == null )
            {
                Assert.True( canConsume );
                Assert.Empty( licenseConsumer.Messages );
            }
            else
            {
                Assert.False( canConsume );
#pragma warning disable CA1307 // Specify StringComparison for clarity
                Assert.Contains( infringingComponent.Name, licenseConsumer.Messages.Single().Text );
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