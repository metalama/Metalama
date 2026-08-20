// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Security.Cryptography;

namespace Metalama.Backstage.Licensing.Licenses;

// Allow weak cryptography
#pragma warning disable CA5384

/// <summary>
/// Provides the authority of the key that signs a test license key and verifies its signature. The key is generated
/// in the current process, so a license key that it signs is valid in the current process only.
/// </summary>
internal sealed class TestLicensingAuthorityProvider : LicensingAuthorityProvider
{
    /// <summary>
    /// The identifier of the single key of the test provider.
    /// </summary>
    public const byte TestKeyId = 255;

    private static readonly Lazy<LicensingAuthority> _testAuthority = new( () => new LicensingAuthority( TestKeyId, DSA.Create() ) );

    /// <summary>
    /// Gets the single authority of the test provider.
    /// </summary>
    /// <remarks>
    /// Every instance of the test provider returns this authority, and the test license key provider signs with it,
    /// so a license key signed in the current process is verified by any service provider of the current process.
    /// </remarks>
    public static LicensingAuthority TestAuthority => _testAuthority.Value;

    /// <summary>
    /// Initializes a new instance of the <see cref="TestLicensingAuthorityProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider that provides the observer of the new provider, or <c>null</c> if the new provider has no observer.</param>
    public TestLicensingAuthorityProvider( IServiceProvider? serviceProvider = null ) : base( serviceProvider, [TestKeyId] ) { }

    /// <inheritdoc />
    protected override LicensingAuthority CreateAuthority( byte keyId ) => TestAuthority;
}
