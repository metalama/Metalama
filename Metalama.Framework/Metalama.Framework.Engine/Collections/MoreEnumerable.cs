// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

#pragma warning disable SA1008, SA1649, SA1600, SA1400, IDE0061, SA1506, SA1117, SA1012, SA1516, SA1124, SA1515

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

// ReSharper disable CommentTypo

#region License and Terms

// MoreLINQ - Extensions to LINQ to Objects
// Copyright (c) 2012 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

// ReSharper disable once CheckNamespace
namespace MoreLinq
{
    [ExcludeFromCodeCoverage]
    internal static class MoreEnumerable
    {
        /// <summary>
        /// Groups the adjacent elements of a sequence according to a
        /// specified key selector function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of
        /// <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by
        /// <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each
        /// element.</param>
        /// <returns>A sequence of groupings where each grouping
        /// (<see cref="IGrouping{TKey,TElement}"/>) contains the key
        /// and the adjacent elements in the same order as found in the
        /// source sequence.</returns>
        /// <remarks>
        /// This method is implemented by using deferred execution and
        /// streams the groupings. The grouping elements, however, are
        /// buffered. Each grouping is therefore yielded as soon as it
        /// is complete and before the next grouping occurs.
        /// </remarks>
        public static IEnumerable<IGrouping<TKey, TSource>> GroupAdjacent<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector )
        {
            return GroupAdjacent( source, keySelector, null );
        }

        /// <summary>
        /// Groups the adjacent elements of a sequence according to a
        /// specified key selector function and compares the keys by using a
        /// specified comparer.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of
        /// <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by
        /// <paramref name="keySelector"/>.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each
        /// element.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to
        /// compare keys.</param>
        /// <returns>A sequence of groupings where each grouping
        /// (<see cref="IGrouping{TKey,TElement}"/>) contains the key
        /// and the adjacent elements in the same order as found in the
        /// source sequence.</returns>
        /// <remarks>
        /// This method is implemented by using deferred execution and
        /// streams the groupings. The grouping elements, however, are
        /// buffered. Each grouping is therefore yielded as soon as it
        /// is complete and before the next grouping occurs.
        /// </remarks>
        private static IEnumerable<IGrouping<TKey, TSource>> GroupAdjacent<TSource, TKey>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            IEqualityComparer<TKey>? comparer )
        {
            if ( source == null )
            {
                throw new ArgumentNullException( nameof(source) );
            }

            if ( keySelector == null )
            {
                throw new ArgumentNullException( nameof(keySelector) );
            }

            return GroupAdjacent( source, keySelector, e => e, comparer );
        }

        /// <summary>
        /// Groups the adjacent elements of a sequence according to a
        /// specified key selector function. The keys are compared by using
        /// a comparer and each group's elements are projected by using a
        /// specified function.
        /// </summary>
        /// <typeparam name="TSource">The type of the elements of
        /// <paramref name="source"/>.</typeparam>
        /// <typeparam name="TKey">The type of the key returned by
        /// <paramref name="keySelector"/>.</typeparam>
        /// <typeparam name="TElement">The type of the elements in the
        /// resulting groupings.</typeparam>
        /// <param name="source">A sequence whose elements to group.</param>
        /// <param name="keySelector">A function to extract the key for each
        /// element.</param>
        /// <param name="elementSelector">A function to map each source
        /// element to an element in the resulting grouping.</param>
        /// <param name="comparer">An <see cref="IEqualityComparer{T}"/> to
        /// compare keys.</param>
        /// <returns>A sequence of groupings where each grouping
        /// (<see cref="IGrouping{TKey,TElement}"/>) contains the key
        /// and the adjacent elements (of type <typeparamref name="TElement"/>)
        /// in the same order as found in the source sequence.</returns>
        /// <remarks>
        /// This method is implemented by using deferred execution and
        /// streams the groupings. The grouping elements, however, are
        /// buffered. Each grouping is therefore yielded as soon as it
        /// is complete and before the next grouping occurs.
        /// </remarks>
        private static IEnumerable<IGrouping<TKey, TElement>> GroupAdjacent<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            IEqualityComparer<TKey>? comparer )
        {
            if ( source == null )
            {
                throw new ArgumentNullException( nameof(source) );
            }

            if ( keySelector == null )
            {
                throw new ArgumentNullException( nameof(keySelector) );
            }

            if ( elementSelector == null )
            {
                throw new ArgumentNullException( nameof(elementSelector) );
            }

            return GroupAdjacentImpl(
                source,
                keySelector,
                elementSelector,
                CreateGroupAdjacentGrouping,
                comparer ?? EqualityComparer<TKey>.Default );
        }

        private static IEnumerable<TResult> GroupAdjacentImpl<TSource, TKey, TElement, TResult>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector,
            Func<TKey, IList<TElement>, TResult> resultSelector,
            IEqualityComparer<TKey> comparer )
        {
            using var iterator = source.GetEnumerator();

            (TKey, List<TElement>) group = default;

            while ( iterator.MoveNext() )
            {
                var key = keySelector( iterator.Current );
                var element = elementSelector( iterator.Current );

                if ( group is ({ } k, { } members) )
                {
                    if ( comparer.Equals( k, key ) )
                    {
                        members.Add( element );

                        continue;
                    }

                    yield return resultSelector( k, members );
                }

                group = (key, [element]);
            }

            {
                if ( group is ({ } k, { } members) )
                {
                    yield return resultSelector( k, members );
                }
            }
        }

        private static IGrouping<TKey, TElement> CreateGroupAdjacentGrouping<TKey, TElement>( TKey key, IList<TElement> members )
            => Grouping.Create( key, members.IsReadOnly ? members : new ReadOnlyCollection<TElement>( members ) );

        private static class Grouping
        {
            public static Grouping<TKey, TElement> Create<TKey, TElement>( TKey key, IEnumerable<TElement> members ) => new( key, members );
        }

#if !NO_SERIALIZATION_ATTRIBUTES
        [Serializable]
        private
#endif
            sealed class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
        {
            private readonly IEnumerable<TElement> _members;

            public Grouping( TKey key, IEnumerable<TElement> members )
            {
                this.Key = key;
                this._members = members;
            }

            public TKey Key { get; }

            public IEnumerator<TElement> GetEnumerator() => this._members.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
        }
    }
}