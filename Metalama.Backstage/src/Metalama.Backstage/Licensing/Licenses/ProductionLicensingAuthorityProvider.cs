// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Backstage.Licensing.Licenses;

/// <summary>
/// Provides the authorities of the public keys that verify the signature of a production license key.
/// </summary>
/// <remarks>
/// The keys 0 and 1 are finite field DSA keys. They sign every license key issued until 2026 and are kept so that
/// those license keys keep verifying. The key 2 is an Elliptic Curve DSA key. It signs the license keys issued
/// afterwards. The identifiers 255 and 254, which the test provider owns, are excluded from this provider.
/// </remarks>
[PublicAPI( "Use in the license key reader utility." )]
public sealed class ProductionLicensingAuthorityProvider : LicensingAuthorityProvider
{
    private static readonly Dictionary<byte, string> _keys = new()
    {
        [0] =
            "<DSAKeyValue><P>9cMyBYBokidciAghqE1POnEbcxpBui3PfazddrQjndkDtPskGvBcjS8LIStB/jR0SICKmLMwl7WoocpdXgYTOopgKJ33E4NOIhc1vbQR6vCCidGWlN88hUKCQJ8cGzme/LDmUT5zfK3TfM6LkMU1fYTNARrefIZkSlg4GGIjZ38=</P><Q>m9h5p2kl1vlwuw12AOQbem3yDXU=</Q><G>pBkhekdI1vk084zMbubnu7qtDyTid6x01crQJiERfmk2HgFt13dXHwei/1kgrRJPWrtZVRKMmO8w+p4jfle82n2/BaFNBLouUoQ/fBYPPdDZBocd/tXqBduF5zq1S12tDv8TIIarMTRtj18F5e68cxBPbweVs4n8meqLEQL5AwA=</G><Y>e2otaOKaVFxnEoHI4g1f7BCcrOaAwd1/GTMkEXGaNw3CYucIuOJdvlZEWa/pa4DTUeK4McHOXRJsZMQdHaoh+dK17NdmMxTa2UMokyoIdayu9kw9TbWUy2zXovJ8CHJVP4RU8wlJk1RKjeMuSK3lYPgo2RTbV9UbU2qK1gmVwg4=</Y><J>AAAAAZOzu4FkAIr0MjlqqHtPNWrFTfjw4/qDWuFvHEf7ioaj8vqRao8mbqsLueqvYIYQ8g8w2WNWFAOG6e8waiQhX2O/DRSZNbc/JfdjQqlPli5be6FqNsGnjKXdEt2boONKU/fpGx/m69V+a/4jxg==</J><Seed>1B0yRR/A/kmE1zMUIFiEMmJ328M=</Seed><PgenCounter>Xg==</PgenCounter></DSAKeyValue>",
        [1] =
            "<DSAKeyValue><P>vAmBC+eZJaZa7HdlTDAgsfcT0QSjqN8d8fEeZ9E1kxfIAYGerlHFHW/A5muBYy8FyO7W8r4mqxpxcvFQEeEqVe89BUXecHjh6FkTEsT25r/nbV4jnZBxNz16qb7A6t8MCr0jzuzrIGFVP5VG/ad0s/1078WqpwQqJQXHmH/lXX0=</P><Q>+RdtGnwCJw4u2H/goSLtaAGr1U0=</Q><G>sxQQgHIuRgYOMtB+r7EGRO/OTRGXhUrFyZ1R9nVerGGC2juEVWSoydr2JquILOwIO7+1kIOwbkhCjNlZIAdvWRlN5COF7gHfPi1dSX7LzDcNbZDADvrOUmk1KG3hZ3Vf67XIbug2/nq8aij7gbEs4eA26EWWpObO0a+e2QmsQII=</G><Y>dP073SH4QG5KiV5BbZEDLiV3/D2eD18D9jsMVD1p+eMZsffU88/Pxfen1Pe5cyulw8gQkEvlAa3GEmGsaGaa7Qp245NPD8fbEOLFu3tdwMhw/ylRHpjTS7BDRjvGeyGwSS0WTWQCwCyI8LN6Rvg7p4RfhHIaAWWkTJNVAG7AN7g=</Y><J>wUCV+9KzxPW+J3/DIm3sIfVf29Z8u5zPXnEZbMTrkWwdgOTSPuXimtiQku8knyWD3iC+GqyhtoFqdgXqQS6WcadAABb2U5mMTL0V1o6Jy6c0cyPb9blmf5wdZxMKVlXe9lcAO8rP16XhQGVs</J><Seed>h7zytTPqA9Ue3F7c/j+9iXW4Ebw=</Seed><PgenCounter>Aag=</PgenCounter></DSAKeyValue>",
        [2] =
            "<ECDSAKeyValue><Curve>nistP256</Curve><X>SZCxgcDOlmWYFLdNGmZcn/MEVLHUPPeAG+37q35Hr48=</X><Y>ascj7FdyMTSXsOfcPJiULv9rMGRPTQEiBwnmWVjRnAE=</Y></ECDSAKeyValue>"
    };

    /// <summary>
    /// Gets the identifiers of all the keys of the production licensing authority, whatever their signature algorithm.
    /// </summary>
    public static ImmutableArray<byte> KeyIdentifiers { get; } = [.. _keys.Keys];

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductionLicensingAuthorityProvider"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider that provides the observer of the new provider, or <c>null</c> if the new provider has no observer.</param>
    public ProductionLicensingAuthorityProvider( IServiceProvider? serviceProvider = null ) : base( serviceProvider, _keys.Keys ) { }

    /// <inheritdoc />
    protected override LicensingAuthority CreateAuthority( byte keyId )
        => keyId switch
        {
            0 or 1 => new DsaLicensingAuthority( keyId, _keys[keyId] ),
            2 => new ECDsaLicensingAuthority( keyId, _keys[keyId] ),
            _ => throw new KeyNotFoundException( $"There is no production licensing authority key of identifier {keyId}." )
        };
}
