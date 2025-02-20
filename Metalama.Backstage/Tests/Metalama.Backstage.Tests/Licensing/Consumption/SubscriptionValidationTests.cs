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
        private static readonly TimeSpan _subscriptionGracePeriod = TimeSpan.FromDays( 30 );

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

        private void AssertPasses( IApplicationInfo applicationInfo, bool requireActiveSubscription = false, string? licenseKey = null )
            => this.TestCore( applicationInfo, requireActiveSubscription, true, null, licenseKey );

        private void AssertFails(
            IApplicationInfo applicationInfo,
            bool requireActiveSubscription = false,
            IComponentInfo? infringingComponent = null,
            string? licenseKey = null )
            => this.TestCore( applicationInfo, requireActiveSubscription, false, infringingComponent, licenseKey );

        private void TestCore(
            IApplicationInfo applicationInfo,
            bool requireActiveSubscription,
            bool mustSucceed,
            IComponentInfo? infringingComponent,
            string? licenseKey )
        {
            licenseKey ??= LicenseKeyProvider.MetalamaProfessionalBusiness;
            var serviceCollection = this.CloneServiceCollection();
            serviceCollection.AddSingleton<IApplicationInfoProvider>( new ApplicationInfoProvider( applicationInfo ) );
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var licenseConsumer = LicenseConsumer.Create(
                LicenseConsumptionOptions.Default with
                {
                    RequireActiveOrGraceSubscription = requireActiveSubscription, SubscriptionGracePeriod = _subscriptionGracePeriod
                },
                serviceProvider,
                [new ExplicitLicenseSource( licenseKey, serviceProvider )] );

            var canConsume = licenseConsumer.TryConsume( _ => true );

            Assert.Equal( mustSucceed, canConsume );

            if ( infringingComponent != null )
            {
#pragma warning disable CA1307 // Specify StringComparison for clarity
                Assert.Contains( infringingComponent.Name, licenseConsumer.Messages.Single().Text );
#pragma warning restore CA1307 // Specify StringComparison for clarity
            }
        }

        [Fact]
        public void PassesWithValidSubscription()
        {
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate );
            this.AssertPasses( applicationInfo );
        }

        [Fact]
        public void FailsWithValidBuildDateButExpiredSubscription()
        {
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate );
            this.Time.Set( LicenseKeyProvider.SubscriptionExpirationDate.Add( _subscriptionGracePeriod ).AddDays( 1 ) );
            this.AssertFails( applicationInfo, true );
        }

        [Fact]
        public void PassesWithValidBuildDateButExpiredSubscriptionWithLegacyKey()
        {
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate );
            this.Time.Set( LicenseKeyProvider.SubscriptionExpirationDate.Add( _subscriptionGracePeriod ).AddDays( 1 ) );
            this.AssertPasses( applicationInfo, true, licenseKey: LicenseKeyProvider.ExpiredSubscriptionLegacyGeneration );
        }

        [Fact]
        public void FailsWithInvalidSubscription()
        {
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate.AddDays( 1 ) );
            this.AssertFails( applicationInfo, false, applicationInfo );
        }

        [Fact]
        public void PassesWithValidSubscriptionForComponentRequiringSubscription()
        {
            var componentInfo = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate, false );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate, componentInfo );
            this.AssertPasses( applicationInfo );
        }

        [Fact]
        public void FailsWithInvalidSubscriptionForComponentRequiringSubscription()
        {
            var componentInfo = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate.AddDays( 1 ), false );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate, componentInfo );
            this.AssertFails( applicationInfo, false, componentInfo );
        }

        [Fact]
        public void PassesWithInvalidSubscriptionForComponentNotRequiringSubscription()
        {
            var componentInfo = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate.AddDays( 1 ), true );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate, componentInfo );
            this.AssertPasses( applicationInfo );
        }

        [Fact]
        public void FailsWithMultipleComponentsAndValidApplication()
        {
            var componentInfo1 = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate.AddDays( 1 ), false );
            var componentInfo2 = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate, false );
            var componentInfo3 = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate.AddDays( 1 ), true );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate, componentInfo1, componentInfo2, componentInfo3 );
            this.AssertFails( applicationInfo, false, componentInfo1 );
        }

        [Fact]
        public void FailsWithMultipleComponentsAndInvalidApplication()
        {
            var componentInfo1 = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate, false );
            var componentInfo2 = CreateComponentInfo( LicenseKeyProvider.SubscriptionExpirationDate.AddDays( 1 ), true );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.SubscriptionExpirationDate.AddDays( 1 ), componentInfo1, componentInfo2 );
            this.AssertFails( applicationInfo, false, applicationInfo );
        }
    }
}