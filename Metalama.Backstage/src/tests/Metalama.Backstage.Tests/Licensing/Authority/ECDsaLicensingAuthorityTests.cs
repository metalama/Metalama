// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Licensing.Licenses;
using System;
using System.Security.Cryptography;
using Xunit;

namespace Metalama.Backstage.Tests.Licensing.Authority;

/// <summary>
/// Tests the licensing authority that signs and verifies with Elliptic Curve DSA over the <c>nistP256</c> curve.
/// </summary>
public sealed class ECDsaLicensingAuthorityTests
{
    /// <summary>
    /// The identifier that the tests of this class give to a key pair that they create themselves. It is not one of
    /// the identifiers that the product uses.
    /// </summary>
    private const byte _testKeyId = 100;

    /// <summary>
    /// The public half of the production key pair of the Elliptic Curve DSA authority, whose identifier is 2. The
    /// tests use it to verify that the implementation parses the key as it is published.
    /// </summary>
    private const string _productionPublicKey =
        "<ECDSAKeyValue><Curve>nistP256</Curve><X>SZCxgcDOlmWYFLdNGmZcn/MEVLHUPPeAG+37q35Hr48=</X><Y>ascj7FdyMTSXsOfcPJiULv9rMGRPTQEiBwnmWVjRnAE=</Y></ECDSAKeyValue>";

    /// <summary>
    /// The public half of a key pair that is used only by <see cref="SignatureOfAnotherPlatformVerifies"/>.
    /// </summary>
    private const string _fixedPublicKey =
        "<ECDSAKeyValue><Curve>nistP256</Curve><X>/lHmCqnJSOGv2mAUAnOioBqzWQenVGMlJCcgpPVO0EE=</X><Y>bsypzhNKP9LEbkxhJFySH886gTLU2WDHzm9gIApzITw=</Y></ECDSAKeyValue>";

    /// <summary>
    /// The message that <see cref="SignatureOfAnotherPlatformVerifies"/> verifies, being the 64 bytes 1 to 64.
    /// </summary>
    private const string _fixedMessage = "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyAhIiMkJSYnKCkqKywtLi8wMTIzNDU2Nzg5Ojs8PT4/QA==";

    /// <summary>
    /// The signature of <see cref="_fixedMessage"/> by the private half of <see cref="_fixedPublicKey"/>. It was
    /// produced on Windows with .NET 10.
    /// </summary>
    private const string _fixedSignature = "7Mx8NyPIwf+4D29OFD/3S0/dQ1TAMdIfEDJHvqF/tk9tQ0oF4fIMuA5Eg+Jrs9Op214pnyZB82wrEe7qJg9h1g==";

    private static byte[] Message => [1, 2, 3];

    /// <summary>
    /// Creates a key pair of the <c>nistP256</c> curve and serializes both halves in the format that the authority
    /// reads. The format is written here by hand, so that the test does not depend on the implementation that it
    /// verifies.
    /// </summary>
    /// <returns>The private half and the public half of a new key pair.</returns>
    private static (string PrivateKey, string PublicKey) CreateKeyPair()
    {
        using var key = ECDsa.Create( ECCurve.NamedCurves.nistP256 );
        var parameters = key.ExportParameters( true );

        var point = $"<Curve>nistP256</Curve><X>{Convert.ToBase64String( parameters.Q.X! )}</X><Y>{Convert.ToBase64String( parameters.Q.Y! )}</Y>";

        return ($"<ECDSAKeyValue>{point}<D>{Convert.ToBase64String( parameters.D! )}</D></ECDSAKeyValue>", $"<ECDSAKeyValue>{point}</ECDSAKeyValue>");
    }

    /// <summary>
    /// Tests that a message signed by the private half of a key pair verifies with the public half.
    /// </summary>
    [Fact]
    public void SignedMessageVerifies()
    {
        var (privateKey, publicKey) = CreateKeyPair();

        var signingAuthority = new ECDsaLicensingAuthority( _testKeyId, privateKey );
        var verifyingAuthority = new ECDsaLicensingAuthority( _testKeyId, publicKey );

        signingAuthority.Sign( Message, out var signature );

        Assert.True( verifyingAuthority.VerifySignature( Message, signature ) );
    }

    /// <summary>
    /// Tests that the signature of a message does not verify against another message.
    /// </summary>
    [Fact]
    public void SignatureOfAnotherMessageDoesNotVerify()
    {
        var (privateKey, publicKey) = CreateKeyPair();

        var signingAuthority = new ECDsaLicensingAuthority( _testKeyId, privateKey );
        var verifyingAuthority = new ECDsaLicensingAuthority( _testKeyId, publicKey );

        signingAuthority.Sign( Message, out var signature );

        Assert.False( verifyingAuthority.VerifySignature( [1, 2, 4], signature ) );
    }

    /// <summary>
    /// Tests that a signature of the <c>nistP256</c> curve is 64 bytes long, which is the length that the growth of
    /// the license key was computed from.
    /// </summary>
    [Fact]
    public void SignatureIsSixtyFourBytesLong()
    {
        var (privateKey, _) = CreateKeyPair();

        new ECDsaLicensingAuthority( _testKeyId, privateKey ).Sign( Message, out var signature );

        Assert.Equal( 64, signature.Length );
    }

    /// <summary>
    /// Tests that a signature produced on one platform verifies on every other one. The hash is shorter than the
    /// field of the curve, and the platforms do not agree on how to extend a short hash, so the implementation pads
    /// it itself. This test fails on a platform whose padding differs from the one of the platform that produced the
    /// signature.
    /// </summary>
    [Fact]
    public void SignatureOfAnotherPlatformVerifies()
    {
        var authority = new ECDsaLicensingAuthority( _testKeyId, _fixedPublicKey );

        Assert.True( authority.VerifySignature( Convert.FromBase64String( _fixedMessage ), Convert.FromBase64String( _fixedSignature ) ) );
    }

    /// <summary>
    /// Tests that the public key of the production authority, as it is published, parses and creates a usable key.
    /// </summary>
    [Fact]
    public void ProductionPublicKeyIsUsable()
    {
        var authority = new ECDsaLicensingAuthority( 2, _productionPublicKey );

        Assert.False( authority.VerifySignature( Message, new byte[64] ) );
    }
}
