// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Backstage.Extensibility;
using System;

namespace Metalama.Backstage.Infrastructure;

internal sealed class RandomNumberGenerator : IBackstageService
{
    private readonly Random _random;

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
}