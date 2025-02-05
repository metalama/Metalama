// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Tests.Licensing.Licenses;
using Metalama.Backstage.Tests.Licensing.LicenseSources;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

public abstract class LicenseConsumptionManagerTestsBase : LicensingTestsBase
{
    private protected LicenseConsumptionManagerTestsBase(
        ITestOutputHelper logger,
        bool isTelemetryEnabled = false )
        : base( logger, isTelemetryEnabled: isTelemetryEnabled ) { }

    private protected TestLicense CreateLicense( string licenseString )
    {
        Assert.True( this.LicenseFactory.TryCreate( licenseString, out var license, out var errorMessage ) );
        Assert.Null( errorMessage );

        return new TestLicense( license );
    }

    private protected ILicenseConsumptionService CreateConsumptionService( TestLicense license )
    {
        // ReSharper disable once CoVariantArrayConversion
        var licenseSource = new TestLicenseSource( "test", license );

        var manager = this.CreateConsumptionManager( licenseSource );

        return manager;
    }

    private protected ILicenseConsumptionService CreateConsumptionManager( params ILicenseSource[] licenseSources )
    {
        return new LicenseConsumptionService( this.ServiceProvider, licenseSources );
    }

    private protected ILicenseConsumptionService CreateConsumptionManager()
    {
        return new LicenseConsumptionService( this.ServiceProvider, Array.Empty<ILicenseSource>() );
    }

    private protected static void TestConsumption(
        ILicenseConsumptionService service,
        LicenseRequirementTestEnum requestedRequirement,
        bool expectedCanConsume )
        => TestConsumption(
            service,
            requestedRequirement,
            "Foo",
            expectedCanConsume );

    private protected static void TestConsumption(
        ILicenseConsumptionService service,
        LicenseRequirementTestEnum requestedRequirement,
        string requiredNamespace,
        bool expectedCanConsume )
    {
        var actualCanConsume = service.CreateConsumer().CanConsume( LicenseRequirementHelper.GetRequirement( requestedRequirement ), requiredNamespace );
        Assert.Equal( expectedCanConsume, actualCanConsume );
    }
}