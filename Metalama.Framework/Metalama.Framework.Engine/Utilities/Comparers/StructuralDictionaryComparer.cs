// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Utilities.Comparers;

internal sealed class StructuralDictionaryComparer<TKey, TValue> : IEqualityComparer<IReadOnlyDictionary<TKey, TValue>>
{
    private readonly IEqualityComparer<TValue> _valueComparer;

    public StructuralDictionaryComparer( IEqualityComparer<TValue> valueComparer )
    {
        this._valueComparer = valueComparer;
    }

    public bool Equals( IReadOnlyDictionary<TKey, TValue>? x, IReadOnlyDictionary<TKey, TValue>? y )
    {
        if ( ReferenceEquals( x, y ) )
        {
            return true;
        }
        else if ( x == null || y == null )
        {
            return false;
        }
        else if ( x.Count != y.Count )
        {
            return false;
        }

        foreach ( var xPair in x )
        {
            if ( !y.TryGetValue( xPair.Key, out var xValue ) )
            {
                return false;
            }

            if ( !this._valueComparer.Equals( xPair.Value, xValue ) )
            {
                return false;
            }
        }

        return true;
    }

    public int GetHashCode( IReadOnlyDictionary<TKey, TValue> obj )
    {
        var hashCode = default(HashCode);

        foreach ( var item in obj )
        {
            hashCode.Add( item.Key );
            hashCode.Add( item.Value );
        }

        return hashCode.ToHashCode();
    }
}