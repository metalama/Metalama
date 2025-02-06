// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Immutable;

namespace Metalama.Patterns.Caching.LoadTests;

internal static class ImmutableArrayExtensions
{
    public static ImmutableArray<T> EmptyIfDefault<T>( this ImmutableArray<T> array ) => array.IsDefault ? ImmutableArray<T>.Empty : array;
}