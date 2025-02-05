// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Engine.Collections
{
    /// <summary>
    /// Provides extension methods to the <see cref="IEnumerable{T}"/> and similar interfaces.
    /// </summary>
    [PublicAPI]
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Appends a set of items to a list.
        /// </summary>
        public static void AddRange<T>( this IList<T> list, IEnumerable<T> items )
        {
            foreach ( var item in items )
            {
                list.Add( item );
            }
        }

        /// <summary>
        /// Builds an <see cref="ImmutableDictionaryOfArray{TKey,TValue}"/> from a collection, with a different value type than the input item type.
        /// </summary>
        public static ImmutableDictionaryOfArray<TKey, TValue> ToMultiValueDictionary<TItem, TKey, TValue>(
            this IEnumerable<TItem> enumerable,
            Func<TItem, TKey> getKey,
            Func<TItem, TValue> getValue,
            IEqualityComparer<TKey>? keyComparer = null )
            where TKey : notnull
            => ImmutableDictionaryOfArray<TKey, TValue>.Create( enumerable, getKey, getValue, keyComparer );

        /// <summary>
        /// Builds an <see cref="ImmutableDictionaryOfArray{TKey,TValue}"/> from a collection, with the same value type than the input item type.
        /// </summary>
        /// <summary>
        /// Builds an <see cref="ImmutableDictionaryOfArray{TKey,TValue}"/> from a collection.
        /// </summary>
        public static ImmutableDictionaryOfArray<TKey, TItem> ToMultiValueDictionary<TItem, TKey>(
            this IEnumerable<TItem> enumerable,
            Func<TItem, TKey> getKey,
            IEqualityComparer<TKey>? keyComparer = null )
            where TKey : notnull
            => enumerable.ToMultiValueDictionary( getKey, i => i, keyComparer );

        public static IReadOnlyCollection<T> Concat<T>( params IReadOnlyCollection<T>[] collections )
        {
            var size = 0;

            foreach ( var collection in collections )
            {
                size += collection.Count;
            }

            var list = new List<T>( size );

            foreach ( var collection in collections )
            {
                list.AddRange( collection );
            }

            return list;
        }

        public static IEnumerable<T> ConcatNotNull<T>( this IEnumerable<T> a, T? b )
            where T : class
            => b == null ? a : a.Concat( b );

        public static IEnumerable<T> AsEnumerable<T>( this Array array )
        {
            for ( var i = 0; i < array.Length; i++ )
            {
                yield return (T) array.GetValue( i )!;
            }
        }
    }
}