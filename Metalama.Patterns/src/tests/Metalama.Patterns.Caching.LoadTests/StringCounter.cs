// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Numerics;

namespace Metalama.Patterns.Caching.LoadTests;

internal sealed class StringCounter
{
    private readonly Dictionary<string, BigInteger> _counters = new();

    private readonly Dictionary<string, List<string>> _details = new();

    public IReadOnlyDictionary<string, BigInteger> Counters => this._counters;

    public IReadOnlyDictionary<string, List<string>> Details => this._details;

    public void Increment( string name, string? detail = null )
    {
        if ( !this._counters.TryGetValue( name, out var count ) )
        {
            this._counters.Add( name, 1 );
        }
        else
        {
            this._counters[name] = count + 1;
        }

        if ( detail == null )
        {
            return;
        }

        if ( !this._details.TryGetValue( name, out var thisDetails ) )
        {
            thisDetails = new List<string>();
            this._details.Add( name, thisDetails );
        }

        thisDetails.Add( detail );
    }
}