// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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