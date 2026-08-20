// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Backstage.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Metalama.Backstage.Licensing.Licenses;

// Allow weak cryptography
#pragma warning disable CA5384, CA5350, CA5351

/// <summary>
/// Utility cryptographic methods for use with the PostSharp licensing system.
/// </summary>
[PublicAPI( "Use in the license generator web and API." )]
public sealed class LicensingAuthority : IBackstageService
{
    // Sharing DSA instances and locking is much faster than having several instances of the DSA object
    // for the same key.
    private static readonly Lazy<LicensingAuthority> _testAuthority = new( () => new LicensingAuthority( (255, () => DSA.Create()) ) );

    private static readonly Lazy<LicensingAuthority> _productionAuthority = new(
        () => new LicensingAuthority(
            (0,
             "<DSAKeyValue><P>9cMyBYBokidciAghqE1POnEbcxpBui3PfazddrQjndkDtPskGvBcjS8LIStB/jR0SICKmLMwl7WoocpdXgYTOopgKJ33E4NOIhc1vbQR6vCCidGWlN88hUKCQJ8cGzme/LDmUT5zfK3TfM6LkMU1fYTNARrefIZkSlg4GGIjZ38=</P><Q>m9h5p2kl1vlwuw12AOQbem3yDXU=</Q><G>pBkhekdI1vk084zMbubnu7qtDyTid6x01crQJiERfmk2HgFt13dXHwei/1kgrRJPWrtZVRKMmO8w+p4jfle82n2/BaFNBLouUoQ/fBYPPdDZBocd/tXqBduF5zq1S12tDv8TIIarMTRtj18F5e68cxBPbweVs4n8meqLEQL5AwA=</G><Y>e2otaOKaVFxnEoHI4g1f7BCcrOaAwd1/GTMkEXGaNw3CYucIuOJdvlZEWa/pa4DTUeK4McHOXRJsZMQdHaoh+dK17NdmMxTa2UMokyoIdayu9kw9TbWUy2zXovJ8CHJVP4RU8wlJk1RKjeMuSK3lYPgo2RTbV9UbU2qK1gmVwg4=</Y><J>AAAAAZOzu4FkAIr0MjlqqHtPNWrFTfjw4/qDWuFvHEf7ioaj8vqRao8mbqsLueqvYIYQ8g8w2WNWFAOG6e8waiQhX2O/DRSZNbc/JfdjQqlPli5be6FqNsGnjKXdEt2boONKU/fpGx/m69V+a/4jxg==</J><Seed>1B0yRR/A/kmE1zMUIFiEMmJ328M=</Seed><PgenCounter>Xg==</PgenCounter></DSAKeyValue>"),
            (1,
             "<DSAKeyValue><P>vAmBC+eZJaZa7HdlTDAgsfcT0QSjqN8d8fEeZ9E1kxfIAYGerlHFHW/A5muBYy8FyO7W8r4mqxpxcvFQEeEqVe89BUXecHjh6FkTEsT25r/nbV4jnZBxNz16qb7A6t8MCr0jzuzrIGFVP5VG/ad0s/1078WqpwQqJQXHmH/lXX0=</P><Q>+RdtGnwCJw4u2H/goSLtaAGr1U0=</Q><G>sxQQgHIuRgYOMtB+r7EGRO/OTRGXhUrFyZ1R9nVerGGC2juEVWSoydr2JquILOwIO7+1kIOwbkhCjNlZIAdvWRlN5COF7gHfPi1dSX7LzDcNbZDADvrOUmk1KG3hZ3Vf67XIbug2/nq8aij7gbEs4eA26EWWpObO0a+e2QmsQII=</G><Y>dP073SH4QG5KiV5BbZEDLiV3/D2eD18D9jsMVD1p+eMZsffU88/Pxfen1Pe5cyulw8gQkEvlAa3GEmGsaGaa7Qp245NPD8fbEOLFu3tdwMhw/ylRHpjTS7BDRjvGeyGwSS0WTWQCwCyI8LN6Rvg7p4RfhHIaAWWkTJNVAG7AN7g=</Y><J>wUCV+9KzxPW+J3/DIm3sIfVf29Z8u5zPXnEZbMTrkWwdgOTSPuXimtiQku8knyWD3iC+GqyhtoFqdgXqQS6WcadAABb2U5mMTL0V1o6Jy6c0cyPb9blmf5wdZxMKVlXe9lcAO8rP16XhQGVs</J><Seed>h7zytTPqA9Ue3F7c/j+9iXW4Ebw=</Seed><PgenCounter>Aag=</PgenCounter></DSAKeyValue>") ) );

    private static readonly SHA1 _sha1 = SHA1.Create();

    private readonly IReadOnlyDictionary<byte, Func<DSA>> _keyFactories;

    /// <summary>
    /// The keys of the current authority, each of them instantiated on first use.
    /// </summary>
    /// <remarks>
    /// A key is instantiated on first use and not in the constructor, because the constructor runs while the licensing
    /// services are registered, whereas an unsigned license requires no signature verification and therefore no key at
    /// all. Finite field DSA is not available on macOS since .NET 11, where instantiating a key throws
    /// <see cref="PlatformNotSupportedException"/>.
    /// </remarks>
    private readonly Dictionary<byte, Lazy<DSA>> _keys;

    private readonly Action? _keyInstantiationObserver;

    private LicensingAuthority( params (int Id, Func<DSA> KeyFactory)[] keys )
        : this( keys.ToDictionary( x => checked((byte) x.Id), x => x.KeyFactory ), null ) { }

    public LicensingAuthority( params IEnumerable<(int Id, string Key)> keys )
        : this(
            keys.ToDictionary(
                x => checked((byte) x.Id),
                x => (Func<DSA>) (() => CryptographyHelper.CreateDsaFromXml( x.Key )) ),
            null ) { }

    private LicensingAuthority( IReadOnlyDictionary<byte, Func<DSA>> keyFactories, Action? keyInstantiationObserver )
    {
        this._keyFactories = keyFactories;
        this._keyInstantiationObserver = keyInstantiationObserver;

        this._keys = keyFactories.ToDictionary( x => x.Key, x => new Lazy<DSA>( () => this.InstantiateKey( x.Value ) ) );
    }

    /// <summary>
    /// Instantiates a key, after reporting the instantiation to the observer of the current authority, if there is one.
    /// </summary>
    /// <param name="keyFactory">The factory of the key to instantiate.</param>
    /// <returns>The new key.</returns>
    /// <remarks>
    /// The observer is invoked before the instantiation and not after it, so that a test observes the attempt even on
    /// a platform where the instantiation throws.
    /// </remarks>
    private DSA InstantiateKey( Func<DSA> keyFactory )
    {
        this._keyInstantiationObserver?.Invoke();

        return keyFactory();
    }

    /// <summary>
    /// Returns a copy of the current authority that has no instantiated key and that invokes
    /// <paramref name="observer"/> each time it instantiates one.
    /// </summary>
    /// <param name="observer">A delegate invoked before each instantiation of a key.</param>
    /// <returns>A new authority with the same keys as the current one.</returns>
    /// <remarks>
    /// A test uses this method to assert that a code path instantiates no key. That assertion cannot be replaced by
    /// running the code path on a platform where DSA is unavailable, because the test suite does not run on such a
    /// platform.
    /// </remarks>
    internal LicensingAuthority WithKeyInstantiationObserver( Action observer ) => new( this._keyFactories, observer );

    internal IEnumerable<byte> KeyIds => this._keys.Keys;

    public static LicensingAuthority GetProductionAuthority() => _productionAuthority.Value;

    public static LicensingAuthority GetTestAuthority() => _testAuthority.Value;

    private static byte[] GetHash( byte[] message )
    {
        lock ( _sha1 )
        {
            return _sha1.ComputeHash( message );
        }
    }

    /// <summary>
    /// Verifies the signature of a message given a <see cref="DSA"/>.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="signature">Signature of <paramref name="message"/> generated with the private key of the current authority.</param>
    /// <returns><c>true</c> if the signature is valid, otherwise <c>false</c>.</returns>
    internal bool VerifySignature( byte[] message, byte keyId, byte[] signature )
    {
        if ( !this._keys.TryGetValue( keyId, out var key ) )
        {
            throw new KeyNotFoundException();
        }

        var dsa = key.Value;

        lock ( dsa )
        {
            return dsa.VerifySignature( GetHash( message ), signature );
        }
    }

    /// <summary>
    /// Gets the key used by the <see cref="Sign"/> method. It requires an authority with a single key.
    /// </summary>
    internal byte SignKeyId => this.KeyIds.Single();

    /// <summary>
    /// Signs a message.
    /// </summary>
    internal void Sign( byte[] message, out byte[] signature )
    {
        var dsa = this._keys.Single().Value.Value;

        lock ( dsa )
        {
            signature = dsa.CreateSignature( GetHash( message ) );
        }
    }
}