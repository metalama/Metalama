// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Security.Cryptography;

namespace Metalama.Backstage.Licensing.Licenses;

// Allow weak cryptography
#pragma warning disable CA5384, CA5350, CA5351

/// <summary>
/// Signs a license key with a single key of the licensing authority, and verifies a signature created with that key.
/// </summary>
/// <remarks>
/// An instance of this class holds a <see cref="DSA"/> object. It is created by an
/// <see cref="ILicensingAuthorityProvider"/>, which creates it only when it is required, because the constructor of
/// the <see cref="DSA"/> object throws on a platform where finite field DSA is unavailable.
/// </remarks>
[PublicAPI( "Use in the license generator web and API." )]
public sealed class LicensingAuthority
{
    private static readonly SHA1 _sha1 = SHA1.Create();

    // Sharing the DSA object and locking is much faster than having several instances of the DSA object for the same key.
    private readonly DSA _key;

    internal LicensingAuthority( byte keyId, DSA key )
    {
        this.KeyId = keyId;
        this._key = key;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LicensingAuthority"/> class from a key represented in XML.
    /// </summary>
    /// <param name="keyId">The identifier of the key.</param>
    /// <param name="key">The XML representation of the key. A private key signs and verifies, a public key only verifies.</param>
    public LicensingAuthority( int keyId, string key ) : this( checked((byte) keyId), CryptographyHelper.CreateDsaFromXml( key ) ) { }

    /// <summary>
    /// Gets the identifier of the key of the current authority.
    /// </summary>
    internal byte KeyId { get; }

    private static byte[] GetHash( byte[] message )
    {
        lock ( _sha1 )
        {
            return _sha1.ComputeHash( message );
        }
    }

    /// <summary>
    /// Verifies the signature of a message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="signature">The signature of <paramref name="message"/>, created with the private key of the current authority.</param>
    /// <returns><c>true</c> if the signature is valid, otherwise <c>false</c>.</returns>
    internal bool VerifySignature( byte[] message, byte[] signature )
    {
        lock ( this._key )
        {
            return this._key.VerifySignature( GetHash( message ), signature );
        }
    }

    /// <summary>
    /// Signs a message.
    /// </summary>
    internal void Sign( byte[] message, out byte[] signature )
    {
        lock ( this._key )
        {
            signature = this._key.CreateSignature( GetHash( message ) );
        }
    }
}
