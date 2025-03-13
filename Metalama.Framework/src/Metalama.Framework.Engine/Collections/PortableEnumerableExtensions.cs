// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

#if !NET6_0_OR_GREATER
using System.Collections.Generic;

// ReSharper disable CheckNamespace
namespace System.Linq
{
    internal static class PortableEnumerableExtensions
    {
        public static HashSet<T> ToHashSet<T>( this IEnumerable<T> collection, IEqualityComparer<T>? comparer = null )
        {
            var hashSet = new HashSet<T>( comparer );

            foreach ( var item in collection )
            {
                hashSet.Add( item );
            }

            return hashSet;
        }
    }
}
#endif