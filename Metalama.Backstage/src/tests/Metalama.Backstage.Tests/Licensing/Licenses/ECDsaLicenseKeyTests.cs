// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Licenses;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Licenses;

/// <summary>
/// Tests a complete license key that is signed with the Elliptic Curve DSA authority. The signature of that
/// authority is 64 bytes long, against 40 bytes for the signature of the finite field DSA authority, so these tests
/// also cover the serialization of the license key with the longer signature.
/// </summary>
public sealed class ECDsaLicenseKeyTests : LicensingTestsBase
{
    /// <summary>
    /// The identifier of the test key pair of the Elliptic Curve DSA authority.
    /// </summary>
    private const byte _testKeyId = 254;

    public ECDsaLicenseKeyTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Creates a license key that requires a signature and signs it with the test key of the Elliptic Curve DSA
    /// authority.
    /// </summary>
    /// <returns>The license key.</returns>
    private static string CreateLicenseKey()
    {
        var builder = new LicenseKeyDataBuilder
        {
            LicenseId = 800,
            Product = LicenseProduct.MetalamaProfessional,
            LicenseType = LicenseType.Business,
            Generation = LicenseGeneration.Current,
            SubscriptionEndDate = LicenseKeyProvider.DefaultSubscriptionExpirationDate
        };

        Assert.True( builder.RequiresSignature() );

        return builder.SignAndSerialize( TestLicensingAuthorityProvider.ECDsaTestAuthority );
    }

    /// <summary>
    /// Tests that a license key signed with the Elliptic Curve DSA test key deserializes and that its signature
    /// verifies through the provider, which selects the authority from the identifier of the key.
    /// </summary>
    [Fact]
    public void SignedLicenseKeyVerifies()
    {
        var licenseKey = CreateLicenseKey();

        Assert.True( LicenseKeyData.TryDeserialize( licenseKey, out var licenseKeyData, out _ ), "Cannot parse." );
        Assert.Equal( _testKeyId, licenseKeyData.SignatureKeyId );
        Assert.True( licenseKeyData.TryVerifySignature( this.LicensingAuthorityProvider, out var signatureErrorMessage ), signatureErrorMessage );
    }

    /// <summary>
    /// Tests that a license key signed with the Elliptic Curve DSA test key is accepted for consumption, which
    /// covers the validation rules that depend on the identifier of the signature key.
    /// </summary>
    [Fact]
    public void SignedLicenseKeyIsConsumable()
    {
        var license = new License( CreateLicenseKey(), this.ServiceProvider );

        Assert.True( license.TryGetConsumptionProperties( LicenseConsumptionOptions.Default, out _, out _ ) );
    }
}
