// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing;
using Metalama.Backstage.Tests.Licensing.LicenseSources;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

public sealed class LicenseUsageTests : LicenseConsumptionManagerTestsBase
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

        var manager = this.CreateConsumptionService( source1, source2 );
        AssertCanConsume( manager, _ => true, true );
        Assert.Equal( 1, license1.NumberOfUses );
        Assert.Equal( 0, license2.NumberOfUses );
        Assert.Equal( 1, source1.NumberOfUses );
        Assert.Equal( 0, source2.NumberOfUses );
    }

    [Fact]
    public void OneOfDifferentLicensesFromMultipleSourcesUsedForForbiddenFeature()
    {
        var license1 = this.CreateInstrumentedLicenseWrapper( LicenseKeyProvider.PostSharpEssentials );
        var source1 = new TestLicenseSource( "source1", license1 );

        var license2 = this.CreateInstrumentedLicenseWrapper( LicenseKeyProvider.MetalamaProfessionalBusiness );
        var source2 = new TestLicenseSource( "source2", license2 );

        var manager = this.CreateConsumptionService( source1, source2 );
        AssertCanConsume( manager, license => license.LicensedProduct == LicensedProduct.MetalamaProfessional, false );
        Assert.Equal( 1, license1.NumberOfUses );
        Assert.Equal( 0, license2.NumberOfUses );
        Assert.Equal( 1, source1.NumberOfUses );
        Assert.Equal( 0, source2.NumberOfUses );
    }
}