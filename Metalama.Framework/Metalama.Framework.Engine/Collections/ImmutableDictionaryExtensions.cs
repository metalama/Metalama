// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Metalama.Framework.Engine.Collections;

internal static class ImmutableDictionaryExtensions
{
    public static void AddOrCreate<TKey, TValue>( [NotNull] ref ImmutableDictionary<TKey, TValue>? dictionary, TKey key, TValue value )
        where TKey : notnull
    {
        dictionary ??= ImmutableDictionary<TKey, TValue>.Empty;

        dictionary = dictionary.Add( key, value );
    }
}