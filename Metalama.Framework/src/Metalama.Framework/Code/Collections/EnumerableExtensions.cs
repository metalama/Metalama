// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Code.Collections
{
    /// <summary>
    /// Extension methods for working with collections of compilation elements.
    /// </summary>
    [PublicAPI]
    [RunTimeOrCompileTime]
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Selects a sequence by recursively applying a function to get the next item, starting from a single initial item.
        /// </summary>
        /// <typeparam name="T">The type of compilation element.</typeparam>
        /// <param name="item">The initial item.</param>
        /// <param name="getNext">A function that returns the next item in the sequence, or <c>null</c> to end the sequence.</param>
        /// <returns>An enumerable sequence starting with <paramref name="item"/> and continuing with items returned by <paramref name="getNext"/>.</returns>
        public static IEnumerable<T> SelectRecursive<T>( T item, Func<T, T?> getNext )
            where T : class, ICompilationElement
            => SelectRecursiveInternal( item, getNext );

        internal static IEnumerable<T> SelectRecursiveInternal<T>( T item, Func<T, T?> getNext )
            where T : class
        {
            for ( var i = item; i != null; i = getNext( i ) )
            {
                yield return i;
            }
        }

        /// <summary>
        /// Selects a sequence by recursively applying a function to get the next item, for each item in a collection.
        /// </summary>
        /// <typeparam name="T">The type of compilation element.</typeparam>
        /// <param name="items">The initial collection of items.</param>
        /// <param name="getNext">A function that returns the next item in the sequence, or <c>null</c> to end the sequence for that item.</param>
        /// <returns>An enumerable sequence containing all items and their descendants obtained by recursively applying <paramref name="getNext"/>.</returns>
        public static IEnumerable<T> SelectRecursive<T>( this IEnumerable<T> items, Func<T, T?> getNext )
            where T : class, ICompilationElement
            => items.SelectRecursiveInternal( getNext );

        internal static IEnumerable<T> SelectRecursiveInternal<T>( this IEnumerable<T> items, Func<T, T?> getNext )
            where T : class
        {
            foreach ( var item in items )
            {
                for ( var i = item; i != null; i = getNext( i ) )
                {
                    yield return i;
                }
            }
        }

        // NOTE: The next method is not public because it pollutes Intellisense and the documentation for all objects.

        /// <summary>
        /// Selects the closure of a graph. This is typically used to select all descendants of a tree node.  This method cannot be
        /// called with a cyclic graph, otherwise a infinite cycle happens.
        /// </summary>
        /// <param name="root">The initial item.</param>
        /// <param name="getChildren">A function that returns the set of all nodes connected to a given node.</param>
        /// <param name="includeRoot">A value indicating whether <paramref name="root"/> itself should be included in the result set.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal static List<T> SelectManyRecursive<T>(
            this T root,
            Func<T, IEnumerable<T>?> getChildren,
            bool includeRoot = false )
            where T : class
        {
            var recursionCheck = 0;

            // Create a list for the results.
            List<T> results = new();

            if ( includeRoot )
            {
                results.Add( root );
            }

            VisitMany( getChildren( root ), getChildren, results, ref recursionCheck );

            return results;
        }

        /// <summary>
        /// Selects the closure of a graph by recursively gathering children for each root item. This is typically used to select all descendants of tree nodes.
        /// </summary>
        /// <typeparam name="T">The type of items in the graph.</typeparam>
        /// <param name="roots">The initial collection of root items.</param>
        /// <param name="getChildren">A function that returns the set of all child nodes connected to a given node.</param>
        /// <param name="includeRoot">A value indicating whether the root items themselves should be included in the result set.</param>
        /// <returns>A list containing the closure of the graph starting from <paramref name="roots"/>.</returns>
        public static List<T> SelectManyRecursive<T>(
            this IEnumerable<T> roots,
            Func<T, IEnumerable<T>?> getChildren,
            bool includeRoot = false )
            where T : class
        {
            var recursionCheck = 0;

            // Create a list for the results.
            List<T> results = new();

            foreach ( var item in roots )
            {
                if ( includeRoot )
                {
                    results.Add( item );
                }

                VisitMany( getChildren( item ), getChildren, results, ref recursionCheck );
            }

            return results;
        }

        /// <summary>
        /// Selects the closure of a graph by recursively gathering children, starting from a single root item, and returns distinct items using reference equality.
        /// </summary>
        /// <typeparam name="T">The type of items in the graph.</typeparam>
        /// <param name="root">The initial root item.</param>
        /// <param name="getChildren">A function that returns the set of all child nodes connected to a given node.</param>
        /// <param name="includeRoot">A value indicating whether <paramref name="root"/> itself should be included in the result set.</param>
        /// <returns>A hash set containing the distinct closure of the graph starting from <paramref name="root"/>.</returns>
        public static HashSet<T> SelectManyRecursiveDistinct<T>(
            this T root,
            Func<T, IEnumerable<T>?> getChildren,
            bool includeRoot = true )
            where T : class
        {
            var recursionCheck = 0;

            HashSet<T> results = new( ReferenceEqualityComparer<T>.Instance );

            if ( includeRoot )
            {
                results.Add( root );
            }

            VisitMany( getChildren( root ), getChildren, results, ref recursionCheck );

            return results;
        }

        /// <summary>
        /// Selects the closure of a graph by recursively gathering children for each root item, and returns distinct items using reference equality.
        /// </summary>
        /// <typeparam name="T">The type of items in the graph.</typeparam>
        /// <param name="roots">The initial collection of root items.</param>
        /// <param name="getChildren">A function that returns the set of all child nodes connected to a given node.</param>
        /// <param name="includeRoots">A value indicating whether the root items themselves should be included in the result set.</param>
        /// <returns>A hash set containing the distinct closure of the graph starting from <paramref name="roots"/>.</returns>
        public static HashSet<T> SelectManyRecursiveDistinct<T>(
            this IEnumerable<T> roots,
            Func<T, IEnumerable<T>?> getChildren,
            bool includeRoots = true )
            where T : class
            => SelectManyRecursiveDistinct( roots, getChildren, ReferenceEqualityComparer<T>.Instance, includeRoots );

        /// <summary>
        /// Selects the closure of a graph by recursively gathering children for each root item, and returns distinct items using a specified equality comparer.
        /// </summary>
        /// <typeparam name="T">The type of items in the graph.</typeparam>
        /// <param name="roots">The initial collection of root items.</param>
        /// <param name="getChildren">A function that returns the set of all child nodes connected to a given node.</param>
        /// <param name="equalityComparer">The equality comparer to use for determining distinct items.</param>
        /// <param name="includeRoots">A value indicating whether the root items themselves should be included in the result set.</param>
        /// <returns>A hash set containing the distinct closure of the graph starting from <paramref name="roots"/>.</returns>
        public static HashSet<T> SelectManyRecursiveDistinct<T>(
            this IEnumerable<T> roots,
            Func<T, IEnumerable<T>?> getChildren,
            IEqualityComparer<T> equalityComparer,
            bool includeRoots = true )
        {
            var recursionCheck = 0;

            HashSet<T> results = new( equalityComparer );

            foreach ( var item in roots )
            {
                if ( includeRoots )
                {
                    results.Add( item );
                }

                VisitMany( getChildren( item ), getChildren, results, ref recursionCheck );
            }

            return results;
        }

        private static void VisitMany<T>(
            IEnumerable<T>? collection,
            Func<T, IEnumerable<T>?> getItems,
            HashSet<T> results,
            ref int recursionCheck )
        {
            recursionCheck++;

            try
            {
                if ( recursionCheck > 64 )
                {
                    throw new InvalidOperationException( "Too many levels of inheritance." );
                }

                if ( collection == null )
                {
                    return;
                }

                foreach ( var item in collection )
                {
                    if ( results.Add( item ) )
                    {
                        VisitMany( getItems( item ), getItems, results, ref recursionCheck );
                    }
                }
            }
            finally
            {
                recursionCheck--;
            }
        }

        private static void VisitMany<T>(
            IEnumerable<T>? collection,
            Func<T, IEnumerable<T>?> getItems,
            List<T> results,
            ref int recursionCheck )
            where T : class
        {
            recursionCheck++;

            try
            {
                if ( recursionCheck > 64 )
                {
                    throw new InvalidOperationException( "Too many levels of inheritance." );
                }

                if ( collection == null )
                {
                    return;
                }

                foreach ( var item in collection )
                {
                    results.Add( item );

                    VisitMany( getItems( item ), getItems, results, ref recursionCheck );
                }
            }
            finally
            {
                recursionCheck--;
            }
        }

        /// <summary>
        /// Filters a sequence of nullable items to include only non-null items.
        /// </summary>
        /// <typeparam name="T">The type of items in the sequence.</typeparam>
        /// <param name="items">The sequence to filter.</param>
        /// <returns>An enumerable sequence containing only non-null items from <paramref name="items"/>.</returns>
        public static IEnumerable<T> WhereNotNull<T>( this IEnumerable<T?> items )
            where T : class
            => items.Where( i => i != null )!;

        // These exist, so that IAttributeCollection.Any overloads don't prevent usage of the Enumerable.Any overloads.

        /// <summary>
        /// Determines whether the attribute collection contains any elements.
        /// </summary>
        /// <param name="attributes">The attribute collection to check.</param>
        /// <returns><c>true</c> if the collection contains any elements; otherwise, <c>false</c>.</returns>
        public static bool Any( this IAttributeCollection attributes ) => Enumerable.Any( attributes );

        /// <summary>
        /// Determines whether any element of the attribute collection satisfies a condition.
        /// </summary>
        /// <param name="attributes">The attribute collection to check.</param>
        /// <param name="predicate">A function to test each element for a condition.</param>
        /// <returns><c>true</c> if any elements in the collection satisfy the condition; otherwise, <c>false</c>.</returns>
        public static bool Any( this IAttributeCollection attributes, Func<IAttribute, bool> predicate ) => Enumerable.Any( attributes, predicate );

        /// <summary>
        /// Caches an enumerable sequence as a read-only list. If the sequence is already a list, it is returned as-is; otherwise, the sequence is enumerated and cached.
        /// </summary>
        /// <typeparam name="T">The type of items in the sequence.</typeparam>
        /// <param name="items">The sequence to cache.</param>
        /// <returns>A read-only list containing the items from <paramref name="items"/>.</returns>
        public static IReadOnlyList<T> Cache<T>( this IEnumerable<T> items ) => items as IReadOnlyList<T> ?? new EnumerableCache<T>( items );

        private class EnumerableCache<T> : IReadOnlyList<T>
        {
            private readonly IEnumerable<T> _underlying;
            private List<T>? _cache;

            public EnumerableCache( IEnumerable<T> underlying )
            {
                this._underlying = underlying;
            }

            private List<T> GetList()
            {
                return this._cache ??= this._underlying.ToList();
            }

            public IEnumerator<T> GetEnumerator() => this.GetList().GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            public int Count => this.GetList().Count;

            public T this[ int index ] => this.GetList()[index];
        }
    }
}