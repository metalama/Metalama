// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Collections;

public static class ImmutableHashSetExtensions
{
    public static ImmutableHashSet<T> AddRange<T>( this ImmutableHashSet<T> hashSet, IEnumerable<T> items )
    {
        foreach ( var item in items )
        {
            hashSet = hashSet.Add( item );
        }

        return hashSet;
    }

    internal static void AddRange<T>( this ImmutableHashSet<T>.Builder hashSetBuilder, IEnumerable<T> items )
    {
        foreach ( var item in items )
        {
            hashSetBuilder.Add( item );
        }
    }
}