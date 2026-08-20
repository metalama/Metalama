// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using Metalama.Backstage.Licensing.Consumption;
using Metalama.Backstage.Licensing.Consumption.Requirements;
using Metalama.Backstage.Licensing.Licenses;
using System.Security.Cryptography;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Metalama.Backstage.Tests.Licensing.Authority;

/// <summary>
/// Tests that <see cref="LicensingAuthority"/> instantiates a DSA object only when it verifies or creates a signature,
/// and not when the licensing services are registered.
/// </summary>
/// <remarks>
/// Finite field DSA is not available on macOS since .NET 11. A DSA object that is instantiated during service
/// registration therefore makes every licence unusable on that platform, including the unsigned ones, which need no
/// signature verification. The unit test suite does not run on that platform, so these tests observe the
/// instantiation of the DSA object instead of relying on the platform to reject it.
/// </remarks>
public sealed class LazyKeyInstantiationTests : LicensingTestsBase
{
    private readonly LicensingAuthority _licensingAuthority;

    private int _keyInstantiationCount;

    public LazyKeyInstantiationTests( ITestOutputHelper logger ) : base( logger )
    {
        this._licensingAuthority = LicensingAuthority.GetProductionAuthority()
            .WithKeyInstantiationObserver( () => Interlocked.Increment( ref this._keyInstantiationCount ) );
    }

    /// <summary>
    /// Gets the licensing authority registered in the service provider of this test. It holds the production keys,
    /// so that the test exercises the same keys as a real process, and it reports every instantiation of a DSA
    /// object to <see cref="KeyInstantiationCount"/>.
    /// </summary>
    protected override LicensingAuthority LicensingAuthority => this._licensingAuthority;

    private int KeyInstantiationCount => Volatile.Read( ref this._keyInstantiationCount );

    [Fact]
    public void ProductionAuthorityInstantiatesNoKeyWhenCreated()
    {
        Assert.Equal( 0, this.KeyInstantiationCount );
    }

    [Fact]
    public void ServiceRegistrationInstantiatesNoKey()
    {
        this.EnsureServicesInitialized();

        Assert.Equal( 0, this.KeyInstantiationCount );
    }

    [Fact]
    public void TrialLicenseInstantiatesNoKey()
    {
        Assert.True( this.LicenseRegistrationService.RegisterTrialEdition().IsSuccess );

        var consumer = this.ServiceProvider.GetRequiredBackstageService<ILicenseConsumptionService>().CreateConsumer();

        Assert.True( consumer.TryConsume( new MetalamaExtensionLicenseRequirement( "<ComponentName>" ) ) );

        Assert.Equal( 0, this.KeyInstantiationCount );
    }

    /// <summary>
    /// Verifies that the observer used by the other tests is actually triggered when a key is instantiated, so that
    /// their assertions cannot pass because the observer is never called at all.
    /// </summary>
    [Fact]
    public void SignatureVerificationInstantiatesKey()
    {
        using var key = DSA.Create();
        var signingAuthority = new LicensingAuthority( (100, key.ToXmlString( true )) );

        byte[] message = [1, 2, 3];
        signingAuthority.Sign( message, out var signature );

        var instantiationCount = 0;

        var verifyingAuthority = new LicensingAuthority( (100, key.ToXmlString( false )) )
            .WithKeyInstantiationObserver( () => instantiationCount++ );

        Assert.Equal( 0, instantiationCount );

        Assert.True( verifyingAuthority.VerifySignature( message, 100, signature ) );

        Assert.Equal( 1, instantiationCount );
    }
}
