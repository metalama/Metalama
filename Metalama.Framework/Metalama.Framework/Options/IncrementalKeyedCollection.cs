// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Options;

#pragma warning disable SA1642

/// <summary>
/// Factory for the generic <see cref="IncrementalKeyedCollection{TKey,TValue}"/> class.
/// </summary>
/// <seealso href="@exposing-options"/>
[CompileTime]
public static class IncrementalKeyedCollection
{
    /// <summary>
    /// Creates a <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the absence of any operation.
    /// </summary>
    public static IncrementalKeyedCollection<TKey, TValue> Empty<TKey, TValue>()
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new( ImmutableDictionary<TKey, IncrementalKeyedCollection<TKey, TValue>.Item>.Empty );

    /// <summary>
    /// Creates a <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the operation of removing any item both in the collection.
    /// </summary>
    public static IncrementalKeyedCollection<TKey, TValue> Clear<TKey, TValue>()
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new( ImmutableDictionary<TKey, IncrementalKeyedCollection<TKey, TValue>.Item>.Empty, true );

    /// <summary>
    /// Creates a <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the operation of adding an item to
    /// the collection, or, if an item with the same key already exists, update this item with the given new values.
    /// </summary>
    public static IncrementalKeyedCollection<TKey, TValue> AddOrApplyChanges<TKey, TValue>( TValue item )
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new(
            ImmutableDictionary.Create<TKey, IncrementalKeyedCollection<TKey, TValue>.Item>()
                .Add( item.Key, new IncrementalKeyedCollection<TKey, TValue>.Item( item ) ) );

    /// <summary>
    /// Creates a <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the operation of adding items to
    /// the collection, or, if any item with the same key already exists, update these item with the given new values.
    /// </summary>
    public static IncrementalKeyedCollection<TKey, TValue> AddOrApplyChanges<TKey, TValue>( params TValue[] items )
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new( items.ToImmutableDictionary( i => i.Key, i => new IncrementalKeyedCollection<TKey, TValue>.Item( i ) ) );

    /// <summary>
    /// Creates a <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the operation of adding items to
    /// the collection, or, if any item with the same key already exists, update these item with the given new values.
    /// </summary>
    public static IncrementalKeyedCollection<TKey, TValue> AddOrApplyChanges<TKey, TValue>( IEnumerable<TValue> items )
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new( items.ToImmutableDictionary( i => i.Key, i => new IncrementalKeyedCollection<TKey, TValue>.Item( i ) ) );

    /// <summary>
    /// Creates a <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the option of removing an item
    /// from the collection.
    /// </summary>
    public static IncrementalKeyedCollection<TKey, TValue> Remove<TKey, TValue>( TValue item )
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new(
            ImmutableDictionary.Create<TKey, IncrementalKeyedCollection<TKey, TValue>.Item>()
                .Add( item.Key, new IncrementalKeyedCollection<TKey, TValue>.Item( item, false ) ) );

    /// <summary>
    /// Creates a <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the option of removing items
    /// from the collection.
    /// </summary>
    public static IncrementalKeyedCollection<TKey, TValue> Remove<TKey, TValue>( params TValue[] items )
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new( items.ToImmutableDictionary( i => i.Key, i => new IncrementalKeyedCollection<TKey, TValue>.Item( i, false ) ) );

    /// <summary>
    /// Creates a <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the option of removing items
    /// from the collection.
    /// </summary>
    public static IncrementalKeyedCollection<TKey, TValue> Remove<TKey, TValue>( IEnumerable<TValue> items )
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new( items.ToImmutableDictionary( i => i.Key, i => new IncrementalKeyedCollection<TKey, TValue>.Item( i, false ) ) );
}