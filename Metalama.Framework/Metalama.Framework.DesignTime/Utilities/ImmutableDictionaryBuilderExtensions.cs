// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

// ReSharper disable once CheckNamespace

namespace System.Collections.Immutable;

internal static class ImmutableDictionaryBuilderExtensions
{
    public static bool TryAdd<TKey, TValue>( this ImmutableDictionary<TKey, TValue>.Builder dictionary, TKey key, TValue value )
        where TKey : notnull
    {
        if ( dictionary.TryGetValue( key, out _ ) )
        {
            return false;
        }
        else
        {
            dictionary.Add( key, value );

            return true;
        }
    }
}