// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Requirements;
using Metalama.Backstage.Licensing.Licenses;
using Metalama.Backstage.Tests.Licensing.Licenses;
using System;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Authority;

/// <summary>
/// Tests that the authority of a production key, and therefore the <see cref="System.Security.Cryptography.DSA"/>
/// object of that key, is created only when a signature is verified, and not when the licensing services are
/// registered or when an unsigned license key is consumed.
/// </summary>
/// <remarks>
/// Finite field DSA is unavailable on macOS since .NET 11. An authority created during service registration
/// therefore makes every license key unusable on that platform, including the unsigned ones, which require no
/// signature verification. The test suite does not run on that platform, so these tests observe the creation of the
/// authority instead of relying on the platform to reject it.
/// </remarks>
public sealed class LazyAuthorityCreationTests : LicensingTestsBase
{
    private readonly TestLicensingAuthorityObserver _observer = new();

    public LazyAuthorityCreationTests( ITestOutputHelper logger ) : base( logger ) { }

    /// <summary>
    /// Registers the provider of the production keys, so that the tests of this class exercise the same keys as a
    /// process of the product.
    /// </summary>
    protected override ILicensingAuthorityProvider CreateLicensingAuthorityProvider( IServiceProvider serviceProvider )
        => new ProductionLicensingAuthorityProvider( serviceProvider );

    /// <inheritdoc />
    protected override void ConfigureServices( ServiceProviderBuilder services )
    {
        base.ConfigureServices( services );

        services.AddSingleton<ILicensingAuthorityObserver>( this._observer );
    }

    [Fact]
    public void ServiceRegistrationCreatesNoAuthority()
    {
        this.EnsureServicesInitialized();

        Assert.Empty( this._observer.CreatedAuthorityKeyIds );
    }

    [Fact]
    public void TrialLicenseCreatesNoAuthority()
    {
        Assert.True( this.LicenseRegistrationService.RegisterTrialEdition().IsSuccess );

        var consumer = this.ServiceProvider.GetRequiredBackstageService<ILicenseConsumptionService>().CreateConsumer();

        Assert.True( consumer.TryConsume( new MetalamaExtensionLicenseRequirement( "<ComponentName>" ) ) );

        Assert.Empty( this._observer.CreatedAuthorityKeyIds );
    }

    /// <summary>
    /// Tests that the observer used by the other tests of this class is called when an authority is actually
    /// created, so that their assertions cannot pass because the observer is never called at all.
    /// </summary>
    [Fact]
    public void SignatureVerificationCreatesAuthority()
    {
        var licenseKeyData = ProductionTestLicenseKeys.Keys.Values
            .Select( GetLicenseKeyData )
            .First( data => data.RequiresSignature() );

        Assert.Empty( this._observer.CreatedAuthorityKeyIds );

        Assert.True( licenseKeyData.TryVerifySignature( this.LicensingAuthorityProvider, out var signatureErrorMessage ), signatureErrorMessage );

        Assert.Equal( licenseKeyData.SignatureKeyId!.Value, Assert.Single( this._observer.CreatedAuthorityKeyIds ) );
    }

    /// <summary>
    /// Deserializes a license key and fails the current test if the license key cannot be deserialized.
    /// </summary>
    /// <param name="licenseKey">The license key.</param>
    /// <returns>The data of <paramref name="licenseKey"/>.</returns>
    private static LicenseKeyData GetLicenseKeyData( string licenseKey )
    {
        Assert.True( LicenseKeyData.TryDeserialize( licenseKey, out var licenseKeyData, out _ ) );

        return licenseKeyData;
    }
}
