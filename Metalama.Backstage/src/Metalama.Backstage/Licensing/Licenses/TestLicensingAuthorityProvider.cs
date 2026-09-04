// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Metalama.Backstage.Licensing.Licenses;

// Allow weak cryptography
#pragma warning disable CA5384

/// <summary>
/// Provides the authorities of the keys that sign a test license key and verify its signature. The keys are generated
/// in the current process, so a license key that they sign is valid in the current process only.
/// </summary>
/// <remarks>
/// There is one key per signature algorithm. The key 255 is a finite field DSA key and the key 254 an Elliptic Curve
/// DSA key. The identifiers 0, 1 and 2, which the production provider owns, are excluded from this provider.
/// </remarks>
internal sealed class TestLicensingAuthorityProvider : LicensingAuthorityProvider
{
    /// <summary>
    /// The identifier of the finite field DSA key of the test provider.
    /// </summary>
    public const byte TestKeyId = 255;

    /// <summary>
    /// The identifier of the Elliptic Curve DSA key of the test provider.
    /// </summary>
    public const byte ECDsaTestKeyId = 254;

    private static readonly Lazy<LicensingAuthority> _testAuthority = new( () => new DsaLicensingAuthority( TestKeyId, DSA.Create() ) );

    private static readonly Lazy<LicensingAuthority> _ecdsaTestAuthority =
        new( () => new ECDsaLicensingAuthority( ECDsaTestKeyId, ECDsa.Create( ECCurve.NamedCurves.nistP256 ) ) );

    /// <summary>
    /// Gets the finite field DSA authority of the test provider.
    /// </summary>
    /// <remarks>
    /// Every instance of the test provider returns this authority, and the test license key provider signs with it,
    /// so a license key signed in the current process is verified by any service provider of the current process.
    /// </remarks>
    public static LicensingAuthority TestAuthority => _testAuthority.Value;

    /// <summary>
    /// Gets the Elliptic Curve DSA authority of the test provider. It is shared in the same way as
    /// <see cref="TestAuthority"/>.
    /// </summary>
    public static LicensingAuthority ECDsaTestAuthority => _ecdsaTestAuthority.Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestLicensingAuthorityProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider that provides the observer of the new provider, or <c>null</c> if the new provider has no observer.</param>
    public TestLicensingAuthorityProvider( IServiceProvider? serviceProvider = null ) : base( serviceProvider, [TestKeyId, ECDsaTestKeyId] ) { }

    /// <inheritdoc />
    protected override LicensingAuthority CreateAuthority( byte keyId )
        => keyId switch
        {
            TestKeyId => TestAuthority,
            ECDsaTestKeyId => ECDsaTestAuthority,
            _ => throw new KeyNotFoundException( $"There is no test licensing authority key of identifier {keyId}." )
        };
}
