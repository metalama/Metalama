// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Collections;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

// ReSharper disable ParameterTypeCanBeEnumerable.Global
// ReSharper disable CheckNamespace

namespace System.Linq;

[PublicAPI]
[RunTimeOrCompileTime]
public static partial class LinqExtensions
{
    [Obsolete( "Use SelectAsReadOnlyCollection or SelectAsArray." )]
    internal static IEnumerable<TOut> Select<TIn, TOut>( this IReadOnlyCollection<TIn> list, Func<TIn, TOut> func ) => SelectAsReadOnlyCollection( list, func );

    [Obsolete( "Use SelectAsList or SelectAsArray." )]
    internal static IEnumerable<TOut> Select<TIn, TOut>( this IReadOnlyList<TIn> list, Func<TIn, TOut> func ) => SelectAsReadOnlyList( list, func );

    public static IReadOnlyCollection<TOut> SelectAsReadOnlyCollection<TIn, TOut>(
        this IReadOnlyCollection<TIn> list,
        Func<TIn, TOut> func,
        bool materialize = false )
    {
        if ( list.Count == 0 )
        {
            return Array.Empty<TOut>();
        }
        else
        {
            return new SelectCollection<TIn, TOut>( list, func );
        }
    }

    public static IReadOnlyList<TOut> SelectAsReadOnlyList<TIn, TOut>( this IReadOnlyList<TIn> list, Func<TIn, TOut> func )
    {
        if ( list.Count == 0 )
        {
            return Array.Empty<TOut>();
        }
        else
        {
            return new SelectList<TIn, TOut>( list, func );
        }
    }

    public static IReadOnlyList<TOut> SelectAsReadOnlyList<TIn, TOut>( this ImmutableArray<TIn> list, Func<TIn, TOut> func )
    {
        if ( list.Length == 0 )
        {
            return Array.Empty<TOut>();
        }
        else
        {
            return new SelectList<TIn, TOut>( list, func );
        }
    }

    public static TOut[] SelectAsArray<TIn, TOut>( this IReadOnlyList<TIn> list, Func<TIn, TOut> func )
    {
        var result = new TOut[list.Count];

        for ( var i = 0; i < result.Length; i++ )
        {
            result[i] = func( list[i] );
        }

        return result;
    }

    public static TOut[] SelectAsArray<TIn, TOut>( this IReadOnlyList<TIn> list, Func<TIn, int, TOut> func )
    {
        var result = new TOut[list.Count];

        for ( var i = 0; i < result.Length; i++ )
        {
            result[i] = func( list[i], i );
        }

        return result;
    }

    public static TOut[] SelectAsArray<TIn, TOut>( this IReadOnlyCollection<TIn> list, Func<TIn, TOut> func )
    {
        var result = new TOut[list.Count];

        var i = 0;

        foreach ( var item in list )
        {
            result[i] = func( item );
            i++;
        }

        return result;
    }

    public static TOut[] SelectAsArray<TIn, TOut>( this ImmutableArray<TIn> list, Func<TIn, TOut> func )
    {
        var result = new TOut[list.Length];

        for ( var i = 0; i < result.Length; i++ )
        {
            result[i] = func( list[i] );
        }

        return result;
    }

    public static List<TOut> SelectAsMutableList<TIn, TOut>( this IReadOnlyCollection<TIn> list, Func<TIn, TOut> func )
    {
        var result = new List<TOut>( list.Count + 4 );

        foreach ( var item in list )
        {
            result.Add( func( item ) );
        }

        return result;
    }

    public static List<TOut> SelectAsMutableList<TIn, TOut>( this IReadOnlyList<TIn> list, Func<TIn, TOut> func )
    {
        var result = new List<TOut>( list.Count + 4 );

        foreach ( var item in list )
        {
            result.Add( func( item ) );
        }

        return result;
    }

    public static List<TOut> SelectAsMutableList<TIn, TOut>( this ImmutableArray<TIn> list, Func<TIn, TOut> func )
    {
        var result = new List<TOut>( list.Length );

        foreach ( var item in list )
        {
            result.Add( func( item ) );
        }

        return result;
    }

    public static ImmutableArray<TOut> SelectAsImmutableArray<TIn, TOut>( this ImmutableArray<TIn> list, Func<TIn, TOut> func )
    {
        var result = ImmutableArray.CreateBuilder<TOut>( list.Length );

        foreach ( var item in list )
        {
            result.Add( func( item ) );
        }

        return result.MoveToImmutable();
    }

    public static ImmutableArray<TOut> SelectAsImmutableArray<TIn, TOut>( this IReadOnlyList<TIn> list, Func<TIn, TOut> func )
    {
        var result = ImmutableArray.CreateBuilder<TOut>( list.Count );

        foreach ( var t in list )
        {
            result.Add( func( t ) );
        }

        return result.MoveToImmutable();
    }

    public static ImmutableArray<TOut> SelectAsImmutableArray<TIn, TOut>( this IReadOnlyCollection<TIn> list, Func<TIn, TOut> func )
    {
        var result = ImmutableArray.CreateBuilder<TOut>( list.Count );

        foreach ( var t in list )
        {
            result.Add( func( t ) );
        }

        return result.MoveToImmutable();
    }

    public static T Min<T>( this ImmutableArray<T> list )
        where T : notnull
        => Min( list, i => i );

    public static TValue Min<TItem, TValue>( this ImmutableArray<TItem> list, Func<TItem, TValue> func, IComparer<TValue>? comparer = null )
        where TValue : notnull
    {
        if ( list.IsDefaultOrEmpty )
        {
            throw new InvalidOperationException( "The sequence is empty." );
        }

        comparer ??= Comparer<TValue>.Default;

        var min = func( list[0] );

        for ( var index = 1; index < list.Length; index++ )
        {
            var value = func( list[index] );

            if ( comparer.Compare( value, min ) < 0 )
            {
                min = value;
            }
        }

        return min;
    }

    public static T Max<T>( this ImmutableArray<T> list )
        where T : notnull
        => Max( list, i => i );

    public static TValue Max<TItem, TValue>( this ImmutableArray<TItem> list, Func<TItem, TValue> func, IComparer<TValue>? comparer = null )
        where TValue : notnull
    {
        if ( list.IsDefaultOrEmpty )
        {
            throw new InvalidOperationException( "The sequence is empty." );
        }

        comparer ??= Comparer<TValue>.Default;

        var max = func( list[0] );

        for ( var index = 1; index < list.Length; index++ )
        {
            var value = func( list[index] );

            if ( comparer.Compare( value, max ) > 0 )
            {
                max = value;
            }
        }

        return max;
    }

    [Obsolete( "This method is redundant." )]
    internal static T[] ToArray<T>( this T[] array ) => array;

    [Obsolete( "Use ToReadOnlyList or ToMutableList" )]
    internal static List<T> ToList<T>( this IReadOnlyList<T> list ) => ToMutableList( list );

    public static List<T> ToMutableList<T>( this IReadOnlyList<T> list )
    {
        var result = new List<T>( list.Count );

        foreach ( var item in list )
        {
            result.Add( item );
        }

        return result;
    }

    public static List<T> ToMutableList<T>( this IReadOnlyCollection<T> list )
    {
        var result = new List<T>( list.Count );

        foreach ( var item in list )
        {
            result.Add( item );
        }

        return result;
    }

#pragma warning disable CMT005
    public static List<T> ToMutableList<T>( this IEnumerable<T> items ) => items.ToList();
#pragma warning restore CMT005

    /// <summary>
    /// Converts an <see cref="IEnumerable{T}"/> to an <see cref="IReadOnlyList{T}"/>, but calls <see cref="Enumerable.ToList{TSource}"/>
    /// only if needed.
    /// </summary>
    public static IReadOnlyList<T> ToReadOnlyList<T>( this IEnumerable<T> items )
    {
        if ( items is IReadOnlyList<T> readOnlyList )
        {
            return readOnlyList;
        }

        using var enumerator = items.GetEnumerator();

        if ( !enumerator.MoveNext() )
        {
            return Array.Empty<T>();
        }

        var list = new List<T> { enumerator.Current };

        while ( enumerator.MoveNext() )
        {
            list.Add( enumerator.Current );
        }

        return list;
    }

    public static IReadOnlyList<T> Materialize<T>( this IReadOnlyList<T> list ) => list is INonMaterialized ? list.ToMutableList() : list;

    public static IReadOnlyCollection<T> Materialize<T>( this IReadOnlyCollection<T> collection )
        => collection is INonMaterialized ? collection.ToMutableList() : collection;

    [Obsolete( "This method is redundant." )]
    internal static IReadOnlyList<T> ToReadOnlyList<T>( this IReadOnlyList<T> list ) => list;

    public static IReadOnlyCollection<T> AsReadOnly<T>( this ICollection<T> collection )
        => collection as IReadOnlyCollection<T> ?? new ReadOnlyCollectionWrapper<T>( collection );

    /// <summary>
    /// Converts an <see cref="IEnumerable"/> to an <see cref="IReadOnlyList{T}"/>, but calls <see cref="Enumerable.ToList{TSource}"/>
    /// only if needed.
    /// </summary>
    public static IReadOnlyList<object> ToReadOnlyList( this IEnumerable collection )
        => collection as IReadOnlyList<object> ?? new List<object>( collection.Cast<object>() );

    [Obsolete( "This method call is redundant." )]
    internal static ImmutableArray<T> ToImmutableArray<T>( this ImmutableArray<T> list ) => list;

    public static ImmutableArray<T> ToImmutableArray<T>( this IReadOnlyList<T> list )
    {
        var builder = ImmutableArray.CreateBuilder<T>( list.Count );

        foreach ( var item in list )
        {
            builder.Add( item );
        }

        return builder.MoveToImmutable();
    }

    public static ImmutableArray<T> ToImmutableArray<T>( this IReadOnlyCollection<T> list )
    {
        var builder = ImmutableArray.CreateBuilder<T>( list.Count );

        foreach ( var item in list )
        {
            builder.Add( item );
        }

        return builder.MoveToImmutable();
    }

    public static ImmutableArray<T> ToImmutableArray<T>( this T[] list ) => ImmutableArray.Create( list );

    public static IEnumerable<T> Concat<T>( this IEnumerable<T> list, T item )
    {
        foreach ( var i in list )
        {
            yield return i;
        }

        yield return item;
    }
    
    public static IReadOnlyCollection<T> Concat<T>( this IReadOnlyCollection<T>? a, IReadOnlyCollection<T>? b )
    {
        if ( a == null || a.Count == 0 )
        {
            return b ?? Array.Empty<T>();
        }

        if ( b == null || b.Count == 0 )
        {
            return a;
        }

        return new ConcatenatedCollection<T>( a, b );
    }

    public static IReadOnlyList<T> Concat<T>( this IReadOnlyList<T>? a, IReadOnlyList<T>? b )
    {
        if ( a == null || a.Count == 0 )
        {
            return b ?? Array.Empty<T>();
        }

        if ( b == null || b.Count == 0 )
        {
            return a;
        }

        return new ConcatenatedList<T>( a, b );
    }

    public static IReadOnlyList<T> Concat<T>( this IReadOnlyList<T>? a, IReadOnlyList<T>? b, IReadOnlyList<T>? c )
    {
        if ( a == null || a.Count == 0 )
        {
            return b.Concat( c );
        }

        if ( b == null || b.Count == 0 )
        {
            return a.Concat( c );
        }

        if ( c == null || c.Count == 0 )
        {
            return a.Concat( b );
        }

        return new ConcatenatedList<T>( a, b, c );
    }

    public static ImmutableArray<T> ConcatAsImmutableArray<T>( this IReadOnlyList<T> a, IReadOnlyList<T> b )
    {
        var result = ImmutableArray.CreateBuilder<T>( a.Count + b.Count );

        foreach ( var item in a )
        {
            result.Add( item );
        }

        foreach ( var item in b )
        {
            result.Add( item );
        }

        return result.MoveToImmutable();
    }

    public static ImmutableArray<T> ConcatAsImmutableArray<T>( this IReadOnlyList<T> a, IReadOnlyList<T> b, IReadOnlyList<T> c )
    {
        var result = ImmutableArray.CreateBuilder<T>( a.Count + b.Count + c.Count );

        foreach ( var item in a )
        {
            result.Add( item );
        }

        foreach ( var item in b )
        {
            result.Add( item );
        }

        foreach ( var item in c )
        {
            result.Add( item );
        }

        return result.MoveToImmutable();
    }

    [Obsolete( "Use ToOrderedList" )]
    internal static List<T> ToList<T>( this IOrderedEnumerable<T> enumerable ) => enumerable.ToMutableList();

    public static List<TItem> ToOrderedList<TItem, TKey>(
        this IEnumerable<TItem> enumerable,
        Func<TItem, TKey> orderBy,
        IComparer<TKey>? comparer = null,
        bool descending = false )
    {
        var list = enumerable.ToMutableList();

        if ( list.Count < 2 )
        {
            return list;
        }

        var multiplier = descending ? -1 : 1;
        comparer ??= Comparer<TKey>.Default;
        list.Sort( ( x, y ) => multiplier * comparer.Compare( orderBy( x ), orderBy( y ) ) );

        return list;
    }

    public static List<TItem> ToOrderedList<TItem, TKey>(
        this IReadOnlyCollection<TItem> enumerable,
        Func<TItem, TKey> orderBy,
        IComparer<TKey>? comparer = null,
        bool descending = false )
    {
        var list = new List<TItem>( enumerable.Count );
        list.AddRange( enumerable );

        if ( list.Count < 2 )
        {
            return list;
        }

        var multiplier = descending ? -1 : 1;
        comparer ??= Comparer<TKey>.Default;

        list.Sort( ( x, y ) => multiplier * comparer.Compare( orderBy( x ), orderBy( y ) ) );

        return list;
    }

    public static bool Any<T>( this IReadOnlyCollection<T> collection ) => collection.Count > 0;

    public static T? FirstOrDefault<T>( this IReadOnlyList<T> list ) => list.Count == 0 ? default : list[0];

    public static T First<T>( this IReadOnlyList<T> list ) => list.Count > 0 ? list[0] : throw new InvalidOperationException();

#if !NET5_0_OR_GREATER
    public static TItem MaxBy<TItem, TValue>( this IEnumerable<TItem> items, Func<TItem, TValue> func, IComparer<TValue>? comparer = null )
    {
        comparer ??= Comparer<TValue>.Default;

        using var enumerator = items.GetEnumerator();

        if ( !enumerator.MoveNext() )
        {
            throw new InvalidOperationException( "The enumerable is empty." );
        }

        var minItem = enumerator.Current;
        var minValue = func( minItem );

        while ( enumerator.MoveNext() )
        {
            var item = enumerator.Current;
            var value = func( item );

            if ( comparer.Compare( value, minValue ) < 0 )
            {
                minValue = value;
                minItem = item;
            }
        }

        return minItem;
    }

    public static TItem MinBy<TItem, TValue>( this IEnumerable<TItem> items, Func<TItem, TValue> func, IComparer<TValue>? comparer = null )
    {
        comparer ??= Comparer<TValue>.Default;

        using var enumerator = items.GetEnumerator();

        if ( !enumerator.MoveNext() )
        {
            throw new InvalidOperationException( "The enumerable is empty." );
        }

        var maxItem = enumerator.Current;
        var maxValue = func( maxItem );

        while ( enumerator.MoveNext() )
        {
            var item = enumerator.Current;
            var value = func( item );

            if ( comparer.Compare( value, maxValue ) > 0 )
            {
                maxValue = value;
                maxItem = item;
            }
        }

        return maxItem;
    }
#endif

    public static TItem? MaxByOrNull<TItem, TValue>( this IEnumerable<TItem> items, Func<TItem, TValue> func, IComparer<TValue>? comparer = null )
        where TItem : class
    {
        comparer ??= Comparer<TValue>.Default;

        using var enumerator = items.GetEnumerator();

        if ( !enumerator.MoveNext() )
        {
            return null;
        }

        var minItem = enumerator.Current;
        var minValue = func( minItem );

        while ( enumerator.MoveNext() )
        {
            var item = enumerator.Current;
            var value = func( item );

            if ( comparer.Compare( value, minValue ) < 0 )
            {
                minValue = value;
                minItem = item;
            }
        }

        return minItem;
    }

    public static TItem? MinByOrNull<TItem, TValue>( this IEnumerable<TItem> items, Func<TItem, TValue> func, IComparer<TValue>? comparer = null )
        where TItem : class
    {
        comparer ??= Comparer<TValue>.Default;

        using var enumerator = items.GetEnumerator();

        if ( !enumerator.MoveNext() )
        {
            return null;
        }

        var maxItem = enumerator.Current;
        var maxValue = func( maxItem );

        while ( enumerator.MoveNext() )
        {
            var item = enumerator.Current;
            var value = func( item );

            if ( comparer.Compare( value, maxValue ) > 0 )
            {
                maxValue = value;
                maxItem = item;
            }
        }

        return maxItem;
    }

    internal static bool Contains<T>( this T[] array, T item ) => Array.IndexOf( array, item ) >= 0;
}