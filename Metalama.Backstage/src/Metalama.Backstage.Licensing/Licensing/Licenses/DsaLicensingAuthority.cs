// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Security.Cryptography;

namespace Metalama.Backstage.Licensing.Licenses;

// Allow weak cryptography
#pragma warning disable CA5384, CA5350, CA5351

/// <summary>
/// Signs a license key with a finite field DSA key and SHA-1, and verifies a signature created with that key. It is
/// the authority of the key identifiers 0, 1 and 255, and therefore of every license key issued until 2026.
/// </summary>
/// <remarks>
/// Finite field DSA is unavailable on macOS since .NET 11, where the constructor of the <see cref="DSA"/> object
/// throws <see cref="System.PlatformNotSupportedException"/>. A license key issued afterwards is signed by
/// <see cref="ECDsaLicensingAuthority"/>, which has no such platform dependency.
/// </remarks>
internal sealed class DsaLicensingAuthority : LicensingAuthority
{
    private static readonly SHA1 _sha1 = SHA1.Create();

    // Sharing the DSA object and locking is much faster than having several instances of the DSA object for the same key.
    private readonly DSA _key;

    internal DsaLicensingAuthority( byte keyId, DSA key ) : base( keyId )
    {
        this._key = key;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DsaLicensingAuthority"/> class from a key represented in XML.
    /// </summary>
    /// <param name="keyId">The identifier of the key.</param>
    /// <param name="key">The XML representation of the key. A private key signs and verifies, a public key only verifies.</param>
    internal DsaLicensingAuthority( int keyId, string key ) : this( checked((byte) keyId), CryptographyHelper.CreateDsaFromXml( key ) ) { }

    private static byte[] GetHash( byte[] message )
    {
        lock ( _sha1 )
        {
            return _sha1.ComputeHash( message );
        }
    }

    /// <inheritdoc />
    internal override bool VerifySignature( byte[] message, byte[] signature )
    {
        lock ( this._key )
        {
            return this._key.VerifySignature( GetHash( message ), signature );
        }
    }

    /// <inheritdoc />
    internal override void Sign( byte[] message, out byte[] signature )
    {
        lock ( this._key )
        {
            signature = this._key.CreateSignature( GetHash( message ) );
        }
    }
}
