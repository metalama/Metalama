// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Metalama.Framework.Code.Collections
{
    [PublicAPI]
    [RunTimeOrCompileTime]
    public static class EnumerableExtensions
    {
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

        public static HashSet<T> SelectManyRecursiveDistinct<T>(
            this IEnumerable<T> roots,
            Func<T, IEnumerable<T>?> getChildren,
            bool includeRoots = true )
            where T : class
            => SelectManyRecursiveDistinct( roots, getChildren, ReferenceEqualityComparer<T>.Instance, includeRoots );

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

        public static IEnumerable<T> WhereNotNull<T>( this IEnumerable<T?> items )
            where T : class
            => items.Where( i => i != null )!;

        // These exist, so that IAttributeCollection.Any overloads don't prevent usage of the Enumerable.Any overloads.
        public static bool Any( this IAttributeCollection attributes ) => Enumerable.Any( attributes );

        public static bool Any( this IAttributeCollection attributes, Func<IAttribute, bool> predicate ) => Enumerable.Any( attributes, predicate );

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