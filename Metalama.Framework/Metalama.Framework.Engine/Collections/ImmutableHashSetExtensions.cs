// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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