// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Licenses;
using System;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Authority;

/// <summary>
/// Tests that a license key whose signature algorithm the current platform does not implement is reported as invalid,
/// instead of raising the <see cref="PlatformNotSupportedException"/> that the creation of the cryptographic object
/// throws. Finite field DSA is unavailable on macOS since .NET 11, so that is what happens there with every license
/// key issued until 2026.
/// </summary>
public sealed class UnsupportedAlgorithmTests : LicensingTestsBase
{
    private const string _expectedErrorMessage = "the license key is signed with a cryptographic algorithm that this platform does not support";

    public UnsupportedAlgorithmTests( ITestOutputHelper logger ) : base( logger ) { }

    protected override ILicensingAuthorityProvider CreateLicensingAuthorityProvider( IServiceProvider serviceProvider )
        => new UnsupportedAlgorithmLicensingAuthorityProvider( serviceProvider );

    /// <summary>
    /// Creates a license key that requires a signature, and signs it with an authority whose algorithm is available,
    /// which is what happened to a license key that was issued before its platform dropped the algorithm.
    /// </summary>
    /// <returns>The license key.</returns>
    private static string CreateSignedLicenseKey()
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

    [Fact]
    public void VerificationReportsTheUnsupportedAlgorithm()
    {
        Assert.True( LicenseKeyData.TryDeserialize( CreateSignedLicenseKey(), out var licenseKeyData, out _ ), "Cannot parse." );

        Assert.False( licenseKeyData.TryVerifySignature( this.LicensingAuthorityProvider, out var errorMessage ) );
        Assert.Equal( _expectedErrorMessage, errorMessage );
    }

    [Fact]
    public void ConsumptionReportsTheUnsupportedAlgorithm()
    {
        var license = new License( CreateSignedLicenseKey(), this.ServiceProvider );

        Assert.False( license.TryGetConsumptionProperties( LicenseConsumptionOptions.Default, out _, out var errorMessage ) );
        Assert.Equal( _expectedErrorMessage, errorMessage );
    }

    [Fact]
    public void RegistrationReportsTheUnsupportedAlgorithm()
    {
        var license = new License( CreateSignedLicenseKey(), this.ServiceProvider );

        Assert.False( license.TryGetRegistrationProperties( out _, out var errorMessage ) );
        Assert.Contains( _expectedErrorMessage, errorMessage, StringComparison.Ordinal );
    }

    /// <summary>
    /// A provider whose keys cannot be instantiated, as the finite field DSA keys cannot be on macOS since .NET 11.
    /// </summary>
    private sealed class UnsupportedAlgorithmLicensingAuthorityProvider : LicensingAuthorityProvider
    {
        public UnsupportedAlgorithmLicensingAuthorityProvider( IServiceProvider serviceProvider ) : base(
            serviceProvider,
            [TestLicensingAuthorityProvider.DsaTestKeyId, TestLicensingAuthorityProvider.ECDsaTestKeyId] ) { }

        protected override LicensingAuthority CreateAuthority( byte keyId )
            => throw new PlatformNotSupportedException( $"The algorithm of the key {keyId} is not supported on this platform." );
    }
}
