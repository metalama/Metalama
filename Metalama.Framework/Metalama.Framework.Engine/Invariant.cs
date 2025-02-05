// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
#if DEBUG
using System.Linq;
#endif
using System.Runtime.CompilerServices;
using NotNullAttribute = System.Diagnostics.CodeAnalysis.NotNullAttribute;

namespace Metalama.Framework.Engine
{
    /// <summary>
    /// A utility class that checks runtime invariant and throws <see cref="AssertionFailedException"/> in case of failure.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [PublicAPI]
    public static class Invariant
    {
        /// <summary>
        /// Checks that a given condition is true and throws an <see cref="AssertionFailedException"/> in case it is not.
        /// </summary>
        /// <param name="condition">The condition that must be true.</param>
#if DEBUG
        [DebuggerStepThrough]
        public static void Assert( [DoesNotReturnIf( false )] bool condition, [CallerArgumentExpression( nameof(condition) )] string? expression = null )
        {
            if ( !condition )
            {
                throw new AssertionFailedException( $"Assert({expression})" );
            }
        }
#else
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        [DebuggerStepThrough]
        public static void Assert( [DoesNotReturnIf( false )] bool condition, string? expression = null ) { }
#endif

        /// <summary>
        /// Checks that a given condition is false and throws an <see cref="AssertionFailedException"/> in case it is not.
        /// </summary>
        /// <param name="condition">The condition that must be true.</param>
#if DEBUG
        [DebuggerStepThrough]
        public static void AssertNot( [DoesNotReturnIf( false )] bool condition, [CallerArgumentExpression( nameof(condition) )] string? expression = null )
        {
            if ( condition )
            {
                throw new AssertionFailedException( $"AssertNot({expression})" );
            }
        }
#else
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        [DebuggerStepThrough]
        public static void AssertNot( [DoesNotReturnIf( false )] bool condition, string? expression = null ) { }
#endif

#if !DEBUG
            [MethodImpl( MethodImplOptions.AggressiveInlining )]
#endif
        [DebuggerStepThrough]
        [return: NotNullIfNotNull( "obj" )]
        public static T? AssertCast<T>( this object? obj )
            where T : class
        {
#if DEBUG
            if ( obj != null && obj is not T )
            {
                throw new AssertionFailedException( $"Can't cast {obj} to {typeof(T)}." );
            }
#endif
            return (T?) obj;
        }

#if DEBUG
        [DebuggerStepThrough]
        public static void Implies(
            bool premise,
            bool conclusion,
            [CallerArgumentExpression( nameof(premise) )]
            string? premiseExpression = null,
            [CallerArgumentExpression( nameof(conclusion) )]
            string? conclusionExpression = null )
        {
            if ( premise && !conclusion )
            {
                throw new AssertionFailedException( $"Implies({premiseExpression}, {conclusionExpression})" );
            }
        }
#else
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        [DebuggerStepThrough]
        public static void Implies( bool premise, bool conclusion )
        {
        }
#endif

#if !DEBUG
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
#endif
        [DebuggerStepThrough]
        public static T Assert<T>( this T obj, Predicate<T> predicate )
            where T : class
        {
#if DEBUG
            if ( !predicate( obj ) )
            {
                throw new AssertionFailedException();
            }
#endif

            return obj;
        }

#if !DEBUG
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
#endif
        [DebuggerStepThrough]
        public static IEnumerable<T> AssertEach<T>( this IEnumerable<T> obj, Predicate<T> predicate )
        {
#if DEBUG
            foreach ( var item in obj )
            {
                if ( !predicate( item ) )
                {
                    throw new AssertionFailedException();
                }

                yield return item;
            }
#else
            return obj;
#endif
        }

        /// <summary>
        /// Checks that a reference is non-null and throws an <see cref="AssertionFailedException"/> if it is not.
        /// </summary>
#if DEBUG
        [DebuggerStepThrough]
        public static T AssertNotNull<T>( [NotNull] this T? obj, [CallerArgumentExpression( nameof(obj) )] string? description = null )
            where T : class
        {
            if ( obj == null )
            {
                throw new AssertionFailedException( $"The reference to {typeof(T).Name} must not be null: '{description}'." );
            }

            return obj;
        }
#else
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        [DebuggerStepThrough]
        public static T AssertNotNull<T>( [NotNull] this T? obj, string? description = null )
            where T : class
        {
#pragma warning disable CS8777
            return obj!;
#pragma warning restore CS8777
        }
#endif

        /// <summary>
        /// Checks that a nullable value is non-null and throws an <see cref="AssertionFailedException"/> if it is not.
        /// </summary>
#if !DEBUG
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
#endif
        [DebuggerStepThrough]
        public static T AssertNotNull<T>( this T? obj, string? description = null )
            where T : struct
        {
#if DEBUG
            if ( obj == null )
            {
                throw new AssertionFailedException( description ?? $"The reference to {typeof(T).Name} must not be null." );
            }

            return obj.Value;
#else
            return obj!.Value;
#endif
        }

#if !DEBUG
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
#endif
        [DebuggerStepThrough]
        public static IEnumerable<T> AssertNoneNull<T>( this IEnumerable<T?>? items )
            where T : class
        {
#if DEBUG
            if ( items == null )
            {
                throw new AssertionFailedException( "The enumeration must not be null." );
            }

            var i = 0;

            foreach ( var item in items )
            {
                if ( item == null )
                {
                    throw new AssertionFailedException( $"The {i}-th {typeof(T).Name} must not be null." );
                }

                i++;

                yield return item;
            }

#else
            return items!;
#endif
        }

#if !DEBUG
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
#endif
        [DebuggerStepThrough]
        public static ImmutableArray<T> AssertNoneNull<T>( this ImmutableArray<T?> items )
            where T : class
        {
#if DEBUG
            if ( items.IsDefault )
            {
                throw new AssertionFailedException( "The enumeration must not be null." );
            }

            for ( var index = 0; index < items.Length; index++ )
            {
                _ = items[index]
                    ?? throw new AssertionFailedException( $"The {index}-th {typeof(T).Name} must not be null." );
            }
#endif
            return items!;
        }

#if !DEBUG
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
#endif
        [DebuggerStepThrough]
        public static T[] AssertNoneNull<T>( this T?[]? items )
            where T : class
        {
#if DEBUG
            for ( var i = 0; i < items.AssertNotNull().Length; i++ )
            {
                if ( items[i] == null )
                {
                    throw new AssertionFailedException( $"The {i}-th {typeof(T).Name} must not be null." );
                }
            }
#endif

            return items!;
        }

#if !DEBUG
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
#endif
        [DebuggerStepThrough]
        public static IEnumerable<T> AssertDistinct<T>( this IEnumerable<T> items )
            where T : class
        {
#if DEBUG
            var itemSet = new HashSet<T>();

            foreach ( var item in items )
            {
                if ( !itemSet.Add( item ) )
                {
                    throw new AssertionFailedException( "The enumeration must not contain equal elements." );
                }

                yield return item;
            }
#else
            return items;
#endif
        }

#if !DEBUG
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
#endif
        [DebuggerStepThrough]
        public static IReadOnlyList<TItem> AssertSorted<TItem>( this IReadOnlyList<TItem> items )
            where TItem : IComparable<TItem>
        {
#if DEBUG
            return AssertSorted( items, static x => x, Comparison );

            static int Comparison( TItem left, TItem right )
            {
                return left.CompareTo( right );
            }
#else
            return items;
#endif
        }

#if !DEBUG
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
#endif
        [DebuggerStepThrough]
        public static IReadOnlyList<TItem> AssertSorted<TItem, TComparable>( this IReadOnlyList<TItem> items, Func<TItem, TComparable> selectComparable )
            where TComparable : IComparable<TComparable>
        {
#if DEBUG
            return AssertSorted( items, selectComparable, Comparison );

            static int Comparison( TComparable left, TComparable right )
            {
                return left.CompareTo( right );
            }
#else
            return items;
#endif
        }

#if !DEBUG
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
#endif
        [DebuggerStepThrough]
        public static IReadOnlyList<TItem> AssertSorted<TItem, TComparable>( this IReadOnlyList<TItem> items, Comparison<TItem> comparison )
        {
#if DEBUG
            return AssertSorted( items, static x => x, comparison );
#else
            return items;
#endif
        }

#if !DEBUG
        [MethodImpl( MethodImplOptions.AggressiveInlining )]
#endif
        [DebuggerStepThrough]
        public static IReadOnlyList<TItem> AssertSorted<TItem, TComparable>(
            this IReadOnlyList<TItem> items,
            Func<TItem, TComparable> selectComparable,
            Comparison<TComparable> comparison )
        {
#if DEBUG
            var materialized = items.Materialize();

            for ( var i = 1; i < materialized.Count; i++ )
            {
                if ( comparison( selectComparable( materialized[i - 1] ), selectComparable( materialized[i] ) ) > 0 )
                {
                    throw new AssertionFailedException( "The enumeration must be sorted according to the given comparison." );
                }
            }

            // Materialized list is intentionally thrown away to allow further assertions on materialization.
            return items;
#else
            return items;
#endif
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static TSymbol AssertSymbolNotNull<TSymbol>(
            this TSymbol? symbol,
            string? justification = null )
            where TSymbol : class, ISymbol
        {
            if ( symbol == null )
            {
                throw new AssertionFailedException( justification ?? $"The reference to {typeof(TSymbol).Name} being null is not supported yet." );
            }

            return symbol;
        }

        [MethodImpl( MethodImplOptions.AggressiveInlining )]
        public static TSymbol AssertSymbolNullNotImplemented<TSymbol>(
            this TSymbol? symbol,
            string? feature )
            where TSymbol : ISymbol
        {
            if ( symbol == null )
            {
                throw new AssertionFailedException(
                    $"The reference to {typeof(TSymbol).Name} must not be null.{(feature != null ? $" Feature: {feature}" : "")}" );
            }

            return symbol;
        }
    }
}