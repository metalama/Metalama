// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Licenses;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Licenses;

public sealed class LegacyLicenseKeyTests : LicensingTestsBase
{
    public LegacyLicenseKeyTests( ITestOutputHelper logger ) : base( logger ) { }

    protected override LicensingAuthority LicensingAuthority => LicensingAuthority.GetProductionAuthority();

    [Theory]
    [MemberData( nameof(GetLicenseKeys) )]
    public void CanReadRealLicenseKey( string licenseKey )
    {
        var authority = LicensingAuthority.GetProductionAuthority();
        Assert.True( LicenseKeyData.TryDeserialize( licenseKey, out var licenseKeyData, out _ ), "Cannot parse." );
        Assert.True( licenseKeyData.VerifySignature( authority ), "Invalid signature." );
    }

    [Theory]
    [MemberData( nameof(GetLicenseKeys) )]
    public void LicenseKeyIsRevoked( string licenseKey )
    {
        var license = new License( licenseKey, this.ServiceProvider );
        Assert.False( license.TryGetConsumptionProperties( LicenseConsumptionOptions.Default, out _, out _ ) );
    }

    [Fact]
    public void HasTestKeys()
    {
        Assert.NotEmpty( ProductionTestLicenseKeys.Keys );
    }

    public static IEnumerable<object[]> GetLicenseKeys() => ProductionTestLicenseKeys.Keys.Values.Select( l => new object[] { l } );
}