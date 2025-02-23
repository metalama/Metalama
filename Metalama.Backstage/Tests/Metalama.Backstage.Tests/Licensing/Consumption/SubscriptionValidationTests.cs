// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Application;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Testing;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
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

        private void AssertPasses( IApplicationInfo applicationInfo, string? licenseKey = null ) => this.TestCore( applicationInfo, true, null, licenseKey );

        private void AssertFails(
            IApplicationInfo applicationInfo,
            IComponentInfo? infringingComponent = null,
            string? licenseKey = null )
            => this.TestCore( applicationInfo, false, infringingComponent, licenseKey );

        private void TestCore(
            IApplicationInfo applicationInfo,
            bool mustSucceed,
            IComponentInfo? infringingComponent,
            string? licenseKey )
        {
            licenseKey ??= LicenseKeyProvider.MetalamaProfessionalBusiness;
            var serviceCollection = this.CloneServiceCollection();
            serviceCollection.AddSingleton<IApplicationInfoProvider>( new ApplicationInfoProvider( applicationInfo ) );
            var serviceProvider = serviceCollection.BuildServiceProvider();

            var messages = new List<LicensingMessage>();

            var licenseConsumer = LicenseConsumer.Create(
                LicenseConsumptionOptions.Default with { SubscriptionGracePeriod = _subscriptionGracePeriod },
                serviceProvider,
                [new ExplicitLicenseSource( licenseKey, serviceProvider )],
                messages.Add );

            var canConsume = licenseConsumer.TryConsume( LicenseRequirement.Any );

            Assert.Equal( mustSucceed, canConsume );

            if ( infringingComponent != null )
            {
#pragma warning disable CA1307 // Specify StringComparison for clarity
                Assert.Contains( infringingComponent.Name, messages.Single().Text );
#pragma warning restore CA1307 // Specify StringComparison for clarity
            }
        }

        [Fact]
        public void PassesWithValidSubscription()
        {
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate );
            this.AssertPasses( applicationInfo );
        }

        [Fact]
        public void FailsWithInvalidSubscription()
        {
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( 1 ) );
            this.AssertFails( applicationInfo, applicationInfo );
        }

        [Fact]
        public void PassesWithValidSubscriptionForComponentRequiringSubscription()
        {
            var componentInfo = CreateComponentInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate, false );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate, componentInfo );
            this.AssertPasses( applicationInfo );
        }

        [Fact]
        public void FailsWithInvalidSubscriptionForComponentRequiringSubscription()
        {
            var componentInfo = CreateComponentInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( 1 ), false );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate, componentInfo );
            this.AssertFails( applicationInfo, componentInfo );
        }

        [Fact]
        public void PassesWithInvalidSubscriptionForComponentNotRequiringSubscription()
        {
            var componentInfo = CreateComponentInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( 1 ), true );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate, componentInfo );
            this.AssertPasses( applicationInfo );
        }

        [Fact]
        public void FailsWithMultipleComponentsAndValidApplication()
        {
            var componentInfo1 = CreateComponentInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( 1 ), false );
            var componentInfo2 = CreateComponentInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate, false );
            var componentInfo3 = CreateComponentInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( 1 ), true );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate, componentInfo1, componentInfo2, componentInfo3 );
            this.AssertFails( applicationInfo, componentInfo1 );
        }

        [Fact]
        public void FailsWithMultipleComponentsAndInvalidApplication()
        {
            var componentInfo1 = CreateComponentInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate, false );
            var componentInfo2 = CreateComponentInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( 1 ), true );
            var applicationInfo = CreateApplicationInfo( LicenseKeyProvider.DefaultSubscriptionExpirationDate.AddDays( 1 ), componentInfo1, componentInfo2 );
            this.AssertFails( applicationInfo, applicationInfo );
        }
    }
}