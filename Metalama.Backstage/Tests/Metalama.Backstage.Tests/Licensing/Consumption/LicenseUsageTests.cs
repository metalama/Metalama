// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Tests.Licensing.LicenseSources;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Consumption;

public class LicenseUsageTests : LicenseConsumptionManagerTestsBase
{
    public LicenseUsageTests( ITestOutputHelper logger )
        : base( logger ) { }

    private static void AssertAllUsed( params IUsable[] usableObjects ) => Assert.Equal( usableObjects.Length, usableObjects.Count( l => l.NumberOfUses > 0 ) );

    [Fact]
    public void FirstOfDifferentLicensesFromMultipleSourcesUsedForAllowedFeature()
    {
        var license1 = this.CreateLicense( LicenseKeyProvider.MetalamaFreePersonal );
        var source1 = new TestLicenseSource( "source1", license1 );

        var license2 = this.CreateLicense( LicenseKeyProvider.MetalamaStarterBusiness );
        var source2 = new TestLicenseSource( "source2", license2 );

        var manager = this.CreateConsumptionManager( source1, source2 );
        TestConsumption( manager, LicenseRequirementTestEnum.Free, true );
        Assert.Equal( 1, license1.NumberOfUses );
        Assert.Equal( 0, license2.NumberOfUses );
        Assert.Equal( 1, source1.NumberOfUses );
        Assert.Equal( 0, source2.NumberOfUses );
    }

    [Fact]
    public void OneOfDifferentLicensesFromMultipleSourcesUsedForForbiddenFeature()
    {
        var license1 = this.CreateLicense( LicenseKeyProvider.MetalamaFreePersonal );
        var source1 = new TestLicenseSource( "source1", license1 );

        var license2 = this.CreateLicense( LicenseKeyProvider.MetalamaStarterBusiness );
        var source2 = new TestLicenseSource( "source2", license2 );

        var manager = this.CreateConsumptionManager( source1, source2 );
        TestConsumption( manager, LicenseRequirementTestEnum.Ultimate, false );
        Assert.Equal( 1, license1.NumberOfUses );
        Assert.Equal( 0, license2.NumberOfUses );
        Assert.Equal( 1, source1.NumberOfUses );
        Assert.Equal( 0, source2.NumberOfUses );
    }

    [Fact]
    public void NamespaceLicenseConsideredForForbiddenFeature()
    {
        var license = this.CreateLicense( LicenseKeyProvider.MetalamaUltimatePersonalProjectBound );
        var manager = this.CreateConsumptionService( license );
        TestConsumption( manager, LicenseRequirementTestEnum.Ultimate, "Foo", false );
        AssertAllUsed( license );
    }
}