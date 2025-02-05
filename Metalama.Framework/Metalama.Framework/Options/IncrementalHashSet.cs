// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Options;

#pragma warning disable SA1642

/// <summary>
/// Factory methods for the <see cref="IncrementalHashSet{T}"/> generic class.
/// </summary>
/// <seealso href="@exposing-options"/>
[CompileTime]
public static class IncrementalHashSet
{
    /// <summary>
    /// Creates  new <see cref="IncrementalHashSet{T}"/> that represents the absence of any operation.
    /// </summary>
    public static IncrementalHashSet<T> Empty<T>()
        where T : notnull
        => IncrementalHashSet<T>.Empty;

    /// <summary>
    /// Creates  new <see cref="IncrementalHashSet{T}"/> that represents the operation clearing
    /// the collection of all items.
    /// </summary>
    public static IncrementalHashSet<T> Clear<T>()
        where T : notnull
        => new( ImmutableDictionary<T, bool>.Empty, true );

    /// <summary>
    /// Creates  new <see cref="IncrementalHashSet{T}"/> that represents the operation of adding an item to
    /// the collection, or to override with a new value if these items already exist.
    /// </summary>
    public static IncrementalHashSet<T> Add<T>( T item )
        where T : notnull
        => new(
            ImmutableDictionary.Create<T, bool>()
                .Add( item, true ) );

    /// <summary>
    /// Creates  new <see cref="IncrementalHashSet{T}"/> that represents the operation of adding items to
    /// the collection, or to override with a new value if these items already exist.
    /// </summary>
    public static IncrementalHashSet<T> Add<T>( params T[] items )
        where T : notnull
        => new( items.ToImmutableDictionary( i => i, _ => true ) );

    /// <summary>
    /// Creates  new <see cref="IncrementalHashSet{T}"/> that represents the operation of adding items to
    /// the collection, or to override with a new value if these items already exist.
    /// </summary>
    public static IncrementalHashSet<T> Add<T>( IEnumerable<T> items )
        where T : notnull
        => new( items.ToImmutableDictionary( i => i, _ => true ) );

    /// <summary>
    /// Creates a <see cref="IncrementalHashSet{T}"/> that represents the option of removing an item
    /// from the collection.
    /// </summary>
    public static IncrementalHashSet<T> Remove<T>( T item )
        where T : notnull
        => new(
            ImmutableDictionary.Create<T, bool>()
                .Add( item, false ) );

    /// <summary>
    /// Creates a <see cref="IncrementalHashSet{T}"/> that represents the option of removing items
    /// from the collection.
    /// </summary>
    public static IncrementalHashSet<T> Remove<T>( params T[] items )
        where T : notnull
        => new( items.ToImmutableDictionary( i => i, _ => false ) );

    /// <summary>
    /// Creates a <see cref="IncrementalHashSet{T}"/> that represents the option of removing items
    /// from the collection.
    /// </summary>
    public static IncrementalHashSet<T> Remove<T>( IEnumerable<T> items )
        where T : notnull
        => new( items.ToImmutableDictionary( i => i, _ => true ) );
}