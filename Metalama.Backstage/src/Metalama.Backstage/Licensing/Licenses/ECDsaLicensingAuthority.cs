// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Security.Cryptography;

namespace Metalama.Backstage.Licensing.Licenses;

/// <summary>
/// Signs a license key with an Elliptic Curve DSA key of the <c>nistP256</c> curve and SHA-256, and verifies a
/// signature created with that key. It is the authority of the key identifiers 2 and 254, and therefore of every
/// license key issued since 2026.
/// </summary>
/// <remarks>
/// <para>
/// Elliptic Curve DSA is available on every platform and every target framework that the product supports, while
/// finite field DSA, which <see cref="DsaLicensingAuthority"/> uses, is unavailable on macOS since .NET 11.
/// </para>
/// <para>
/// The signature of the <c>nistP256</c> curve is 64 bytes long, against 40 bytes for the signature of
/// <see cref="DsaLicensingAuthority"/>. The <c>Signature</c> field of the license key is preceded by its length, so
/// the format of the license key is unchanged and only its Base32 representation grows.
/// </para>
/// </remarks>
[PublicAPI( "Use in the license generator web and API." )]
public sealed class ECDsaLicensingAuthority : LicensingAuthority
{
    /// <summary>
    /// The number of bytes that the digest of the signed message is truncated to. It is the length of the SHA-1 hash
    /// that <see cref="DsaLicensingAuthority"/> signs. A hash of 160 bits gives a collision resistance of 80 bits,
    /// which is sufficient here, because the licensing authority produces the signed message itself, so an attacker
    /// who wants to exploit a collision must first have the authority sign one of the two colliding messages.
    /// </summary>
    private const int _hashLength = 20;

    /// <summary>
    /// The size in bytes of the field of the <c>nistP256</c> curve.
    /// </summary>
    private const int _fieldLength = 32;

    private static readonly SHA256 _sha256 = SHA256.Create();

    // Sharing the ECDsa object and locking is much faster than having several instances of the ECDsa object for the same key.
    private readonly ECDsa _key;

    internal ECDsaLicensingAuthority( byte keyId, ECDsa key ) : base( keyId )
    {
        this._key = key;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ECDsaLicensingAuthority"/> class from a key represented in XML.
    /// </summary>
    /// <param name="keyId">The identifier of the key.</param>
    /// <param name="key">The XML representation of the key. A private key signs and verifies, a public key only verifies.</param>
    public ECDsaLicensingAuthority( int keyId, string key ) : this( checked((byte) keyId), CryptographyHelper.CreateECDsaFromXml( key ) ) { }

    /// <summary>
    /// Computes the value that is signed and verified, being the SHA-256 digest of the message, truncated to
    /// <see cref="_hashLength"/> bytes and then padded on the left with zeros up to <see cref="_fieldLength"/> bytes.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <returns>The padded hash of <paramref name="message"/>.</returns>
    /// <remarks>
    /// The padding produces the same integer as the conversion of a truncated hash defined by SEC 1, and it is applied
    /// here instead of passing a hash shorter than the field, because the platforms do not agree on how to extend such
    /// a hash. Without it, a signature created on one platform would not necessarily verify on another one.
    /// </remarks>
    private static byte[] GetHash( byte[] message )
    {
        byte[] digest;

        lock ( _sha256 )
        {
            digest = _sha256.ComputeHash( message );
        }

        var hash = new byte[_fieldLength];
        Array.Copy( digest, 0, hash, _fieldLength - _hashLength, _hashLength );

        return hash;
    }

    /// <inheritdoc />
    internal override bool VerifySignature( byte[] message, byte[] signature )
    {
        lock ( this._key )
        {
            return this._key.VerifyHash( GetHash( message ), signature );
        }
    }

    /// <inheritdoc />
    internal override void Sign( byte[] message, out byte[] signature )
    {
        lock ( this._key )
        {
            signature = this._key.SignHash( GetHash( message ) );
        }
    }
}
