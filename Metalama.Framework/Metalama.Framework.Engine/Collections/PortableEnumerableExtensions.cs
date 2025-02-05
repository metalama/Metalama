// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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