// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Tests.Licensing.LicenseSources;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

public sealed class LicenseUsageTests : LicenseConsumptionServiceTestsBase
{
    public LicenseUsageTests( ITestOutputHelper logger )
        : base( logger ) { }

    [Fact]
    public void FirstOfDifferentLicensesFromMultipleSourcesUsedForAllowedFeature()
    {
        var license1 = this.CreateInstrumentedLicenseWrapper( LicenseKeyProvider.MetalamaCommunity );
        var source1 = new TestLicenseSource( "source1", license1 );

        var license2 = this.CreateInstrumentedLicenseWrapper( LicenseKeyProvider.MetalamaProfessionalBusiness );
        var source2 = new TestLicenseSource( "source2", license2 );

        var service = this.CreateConsumptionService( source1, source2 );
        AssertCanConsume( service, LicenseRequirement.Any, true );
        Assert.Equal( 1, license1.NumberOfUses );
        Assert.Equal( 0, license2.NumberOfUses );
        Assert.Equal( 1, source1.NumberOfUses );
        Assert.Equal( 1, source2.NumberOfUses );
    }

    [Fact]
    public void OneOfDifferentLicensesFromMultipleSourcesUsedForForbiddenFeature()
    {
        var license1 = this.CreateInstrumentedLicenseWrapper( LicenseKeyProvider.PostSharpEssentials );
        var source1 = new TestLicenseSource( "source1", license1 );

        var license2 = this.CreateInstrumentedLicenseWrapper( LicenseKeyProvider.MetalamaProfessionalBusiness );
        var source2 = new TestLicenseSource( "source2", license2 );

        var service = this.CreateConsumptionService( source1, source2 );
        AssertCanConsume( service, new DelegateLicenseRequirement( context => context.License.LicenseProduct == LicenseProduct.MetalamaProfessional ), true );
        Assert.Equal( 0, license1.NumberOfUses );
        Assert.Equal( 1, license2.NumberOfUses );
        Assert.Equal( 1, source1.NumberOfUses );
        Assert.Equal( 1, source2.NumberOfUses );
    }
}