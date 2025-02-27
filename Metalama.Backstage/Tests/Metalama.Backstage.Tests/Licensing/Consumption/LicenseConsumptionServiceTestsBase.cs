// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Sources;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Testing;
using Metalama.Backstage.Tests.Licensing.Licenses;
using Metalama.Backstage.Tests.Licensing.LicenseSources;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

public abstract class LicenseConsumptionServiceTestsBase : LicensingTestsBase
{
    private protected LicenseConsumptionServiceTestsBase(
        ITestOutputHelper logger,
        bool isTelemetryEnabled = false )
        : base( logger, isTelemetryEnabled: isTelemetryEnabled ) { }

    protected void SetBuildDate( DateTime buildDate )
    {
        ((TestApplicationInfo) this.ApplicationInfo).BuildDate = buildDate;
    }

    private protected InstrumentedLicenseWrapper CreateInstrumentedLicenseWrapper( string licenseString )
    {
        var licenseFactory = new LicenseFactory( this.ServiceProvider );
        Assert.True( licenseFactory.TryCreate( licenseString, out var license, out var errorMessage ) );
        Assert.Null( errorMessage );

        return new InstrumentedLicenseWrapper( license );
    }

    private protected ILicenseConsumptionService CreateConsumptionService( InstrumentedLicenseWrapper license )
    {
        // ReSharper disable once CoVariantArrayConversion
        var licenseSource = new TestLicenseSource( "test", license );

        var manager = this.CreateConsumptionService( licenseSource );

        return manager;
    }

    private protected ILicenseConsumptionService CreateConsumptionService( string licenseKey )
        => this.CreateConsumptionService( this.CreateInstrumentedLicenseWrapper( licenseKey ) );

    private protected ILicenseConsumptionService CreateConsumptionService( params ILicenseSource[] licenseSources )
    {
        return new LicenseConsumptionService( this.ServiceProvider, licenseSources );
    }

    private protected static void AssertCanConsume(
        ILicenseConsumptionService service,
        LicenseRequirement requirement,
        bool expectedCanConsume )
    {
        var actualCanConsume = service.CreateConsumer().TryConsume( requirement );
        Assert.Equal( expectedCanConsume, actualCanConsume );
    }
}