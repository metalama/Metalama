// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;

namespace Metalama.Backstage.Licensing.Licenses;

/// <summary>
/// Signs a license key with a single key of the licensing authority, and verifies a signature created with that key.
/// </summary>
/// <remarks>
/// <para>
/// There are two implementations, one for each signature algorithm, both internal to this assembly. One signs with
/// finite field DSA and is the algorithm of every license key issued until 2026. The other signs with Elliptic Curve
/// DSA and is the algorithm of the license keys issued afterwards. The identifier of the key selects the
/// implementation, because the two sets of identifiers are disjoint. An authority is therefore always obtained from
/// an <see cref="ILicensingAuthorityProvider"/> and never constructed directly.
/// </para>
/// <para>
/// An instance of this class holds the cryptographic key object. It is created by an
/// <see cref="ILicensingAuthorityProvider"/>, which creates it only when it is required, because the constructor of a
/// finite field DSA key throws on a platform where that algorithm is unavailable.
/// </para>
/// </remarks>
[PublicAPI( "Use in the license generator web and API." )]
public abstract class LicensingAuthority
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LicensingAuthority"/> class.
    /// </summary>
    /// <param name="keyId">The identifier of the key.</param>
    private protected LicensingAuthority( byte keyId )
    {
        this.KeyId = keyId;
    }

    /// <summary>
    /// Creates the authority of a key given in its XML representation, choosing the implementation from the root
    /// element of that representation.
    /// </summary>
    /// <param name="keyId">The identifier of the key.</param>
    /// <param name="key">The XML representation of the key. A private key signs and verifies, a public key only verifies.</param>
    /// <returns>The authority of the key.</returns>
    /// <exception cref="ArgumentException">The root element of <paramref name="key"/> is neither <c>DSAKeyValue</c> nor <c>ECDSAKeyValue</c>.</exception>
    internal static LicensingAuthority Create( byte keyId, string key )
        => CryptographyHelper.GetKeyRootElementName( key ) switch
        {
            "DSAKeyValue" => new DsaLicensingAuthority( keyId, key ),
            "ECDSAKeyValue" => new ECDsaLicensingAuthority( keyId, key ),
            var name => throw new ArgumentException( $"Invalid key. Unknown root element: {name}", nameof(key) )
        };

    /// <summary>
    /// Gets the identifier of the key of the current authority.
    /// </summary>
    internal byte KeyId { get; }

    /// <summary>
    /// Verifies the signature of a message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="signature">The signature of <paramref name="message"/>, created with the private key of the current authority.</param>
    /// <returns><c>true</c> if the signature is valid, otherwise <c>false</c>.</returns>
    internal abstract bool VerifySignature( byte[] message, byte[] signature );

    /// <summary>
    /// Signs a message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="signature">At output, the signature of <paramref name="message"/>.</param>
    internal abstract void Sign( byte[] message, out byte[] signature );
}
