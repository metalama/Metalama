// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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