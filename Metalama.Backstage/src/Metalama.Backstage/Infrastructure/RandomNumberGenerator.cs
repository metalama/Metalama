// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Infrastructure;

internal sealed class RandomNumberGenerator : IBackstageService
{
    private readonly Random _random;

    // A cryptographically-secure RNG, used for security-sensitive values. It is thread-safe, so unlike _random it
    // does not need locking. It is never seeded, so security-sensitive values are never predictable.
    private readonly System.Security.Cryptography.RandomNumberGenerator _cryptographicRandom =
        System.Security.Cryptography.RandomNumberGenerator.Create();

    public RandomNumberGenerator( int? seed = null )
    {
        this._random = seed != null ? new Random( seed.Value ) : new Random();
    }

    // ReSharper disable once UnusedMember.Global
    public int NextInt32()
    {
        lock ( this._random )
        {
            return this._random.Next();
        }
    }

    public long NextInt64()
    {
        lock ( this._random )
        {
            return ((long) this._random.Next() << 32) | (uint) this._random.Next();
        }
    }

    public Guid NextGuid()
    {
        lock ( this._random )
        {
            var bytes = new byte[16];
            this._random.NextBytes( bytes );

            return new Guid( bytes );
        }
    }

    public void NextBytes( byte[] buffer )
    {
        lock ( this._random )
        {
            this._random.NextBytes( buffer );
        }
    }

    /// <summary>
    /// Fills <paramref name="buffer"/> with cryptographically-secure random bytes. Unlike <see cref="NextBytes"/>,
    /// this method is not backed by <see cref="Random"/> and must be used for security-sensitive values such as
    /// encryption keys and salts. The value is independent of the seed passed to the constructor.
    /// </summary>
    public void NextCryptographicBytes( byte[] buffer )
    {
        this._cryptographicRandom.GetBytes( buffer );
    }

    /// <summary>
    /// Returns a cryptographically-secure random <see cref="long"/>. Unlike <see cref="NextInt64"/>, this method is
    /// not backed by <see cref="Random"/> and must be used for security-sensitive values. The value is independent
    /// of the seed passed to the constructor.
    /// </summary>
    public long NextCryptographicInt64()
    {
        var bytes = new byte[sizeof(long)];
        this.NextCryptographicBytes( bytes );

        return BitConverter.ToInt64( bytes, 0 );
    }
}