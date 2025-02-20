// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Backstage.Configuration;
using Metalama.Backstage.Licensing;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Registration;

#pragma warning disable CS0612, CS0618 // Type or member is obsolete

public sealed class LegacyLicenseRegistrationTests : LicensingTestsBase
{
    public LegacyLicenseRegistrationTests( ITestOutputHelper logger ) : base( logger ) { }

    [Fact]
    public void FreeLicenseIsNotOverwrittenByCommunity()
    {
        Assert.True( this.LicenseRegistrationService.TryRegisterLicense( LicenseKeyProvider.MetalamaFree, out _ ) );
        Assert.Single( this.LicenseRegistrationService.RegisteredLicenses );
        Assert.True( this.LicenseRegistrationService.TryRegisterLicense( LicenseKeyProvider.MetalamaCommunity, out _ ) );

        var registeredLicenses = this.LicenseRegistrationService.RegisteredLicenses.ToList();
        Assert.Equal( 2, registeredLicenses.Count );
        Assert.Contains( registeredLicenses, l => l.Product == LicensedProduct.MetalamaFree );
        Assert.Contains( registeredLicenses, l => l.Product == LicensedProduct.MetalamaCommunity );
    }

    [Fact]
    public void CommunityLicenseIsNotOverwrittenByFree()
    {
        Assert.True( this.LicenseRegistrationService.TryRegisterLicense( LicenseKeyProvider.MetalamaCommunity, out _ ) );
        Assert.Single( this.LicenseRegistrationService.RegisteredLicenses );
        Assert.True( this.LicenseRegistrationService.TryRegisterLicense( LicenseKeyProvider.MetalamaFree, out _ ) );

        var registeredLicenses = this.LicenseRegistrationService.RegisteredLicenses.ToList();
        Assert.Equal( 2, registeredLicenses.Count );
        Assert.Contains( registeredLicenses, l => l.Product == LicensedProduct.MetalamaFree );
        Assert.Contains( registeredLicenses, l => l.Product == LicensedProduct.MetalamaCommunity );
    }

    [Fact]
    public void MetalamaCommunityNotStoredInLegacyField()
    {
        Assert.True( this.LicenseRegistrationService.TryRegisterLicense( LicenseKeyProvider.MetalamaCommunity, out _ ) );

        var configuration = this.ConfigurationManager!.Get<LicensingConfiguration>();

        Assert.Null( configuration.LegacyLicense );
        Assert.Single( configuration.Licenses );
        Assert.Contains( LicenseKeyProvider.MetalamaCommunity, configuration.Licenses );
    }

    [Fact]
    public void MetalamaProfessionalStoredInLegacyField()
    {
        Assert.True( this.LicenseRegistrationService.TryRegisterLicense( LicenseKeyProvider.MetalamaProfessionalBusiness, out _ ) );

        var configuration = this.ConfigurationManager!.Get<LicensingConfiguration>();

        Assert.Equal( LicenseKeyProvider.MetalamaProfessionalBusiness, configuration.LegacyLicense );
    }
}