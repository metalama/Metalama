// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;

namespace Metalama.Framework.Engine.Utilities.Threading;

public sealed class RandomNumberProvider : IRandomNumberProvider
{
    private readonly Random _random;

    public RandomNumberProvider()
    {
        this._random = new Random();
    }

    public RandomNumberProvider( int seed )
    {
        this._random = new Random( seed );
    }

    public int GetNextInt()
    {
        lock ( this._random )
        {
            return this._random.Next();
        }
    }

    public double GetNextDouble()
    {
        lock ( this._random )
        {
            return this._random.NextDouble();
        }
    }
}