// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Options;

#pragma warning disable SA1642

/// <summary>
/// Factory for the generic <see cref="IncrementalKeyedCollection{TKey,TValue}"/> class.
/// </summary>
/// <remarks>
/// <para>
/// This static class provides convenient factory methods for creating <see cref="IncrementalKeyedCollection{TKey,TValue}"/> instances.
/// These methods are useful when constructing incremental keyed collections as properties within hierarchical options classes.
/// </para>
/// <para>
/// Incremental keyed collections are typically used in option classes when you need a property that represents a collection of
/// complex items (each implementing <see cref="IIncrementalKeyedCollectionItem{TKey}"/>) that can be added, updated, or removed
/// across different configuration layers. For example, you might have a collection of caching policy rules where each rule is
/// identified by a key and can be refined at different levels.
/// </para>
/// </remarks>
/// <seealso cref="IncrementalKeyedCollection{TKey,TValue}"/>
/// <seealso cref="IIncrementalKeyedCollectionItem{TKey}"/>
/// <seealso cref="IHierarchicalOptions"/>
/// <seealso href="@exposing-options"/>
[CompileTime]
public static class IncrementalKeyedCollection
{
    /// <summary>
    /// Creates a new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the absence of any operation.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of values in the collection.</typeparam>
    /// <returns>An empty <see cref="IncrementalKeyedCollection{TKey,TValue}"/> representing no changes to the collection.</returns>
    /// <remarks>
    /// This is equivalent to not setting the property at all. When merged with another collection, the other collection's operations take precedence.
    /// </remarks>
    public static IncrementalKeyedCollection<TKey, TValue> Empty<TKey, TValue>()
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new( ImmutableDictionary<TKey, IncrementalKeyedCollection<TKey, TValue>.Item>.Empty );

    /// <summary>
    /// Creates a new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the operation of clearing
    /// all items from the collection.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of values in the collection.</typeparam>
    /// <returns>An <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that, when merged, clears all previous items from the collection.</returns>
    /// <remarks>
    /// This operation discards all items from previous layers. It is useful when you want to start with an empty collection
    /// at a specific configuration level, ignoring all inherited items.
    /// </remarks>
    public static IncrementalKeyedCollection<TKey, TValue> Clear<TKey, TValue>()
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new( ImmutableDictionary<TKey, IncrementalKeyedCollection<TKey, TValue>.Item>.Empty, true );

    /// <summary>
    /// Creates a new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the operation of adding an item to
    /// the collection, or merging it with an existing item if the same key already exists.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of values in the collection.</typeparam>
    /// <param name="item">The item to add or merge into the collection.</param>
    /// <returns>An <see cref="IncrementalKeyedCollection{TKey,TValue}"/> representing the add or merge operation.</returns>
    /// <remarks>
    /// If an item with the same key exists in a previous layer, the two items are merged using <see cref="IIncrementalObject.ApplyChanges"/>.
    /// </remarks>
    public static IncrementalKeyedCollection<TKey, TValue> AddOrApplyChanges<TKey, TValue>( TValue item )
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new(
            ImmutableDictionary.Create<TKey, IncrementalKeyedCollection<TKey, TValue>.Item>()
                .Add( item.Key, new IncrementalKeyedCollection<TKey, TValue>.Item( item ) ) );

    /// <summary>
    /// Creates a new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the operation of adding items to
    /// the collection, or merging them with existing items if the same keys already exist.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of values in the collection.</typeparam>
    /// <param name="items">The items to add or merge into the collection.</param>
    /// <returns>An <see cref="IncrementalKeyedCollection{TKey,TValue}"/> representing the add or merge operations.</returns>
    /// <remarks>
    /// For each item, if an item with the same key exists in a previous layer, the two items are merged using <see cref="IIncrementalObject.ApplyChanges"/>.
    /// </remarks>
    public static IncrementalKeyedCollection<TKey, TValue> AddOrApplyChanges<TKey, TValue>( params TValue[] items )
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new( items.ToImmutableDictionary( i => i.Key, i => new IncrementalKeyedCollection<TKey, TValue>.Item( i ) ) );

    /// <summary>
    /// Creates a new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the operation of adding items to
    /// the collection, or merging them with existing items if the same keys already exist.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of values in the collection.</typeparam>
    /// <param name="items">The items to add or merge into the collection.</param>
    /// <returns>An <see cref="IncrementalKeyedCollection{TKey,TValue}"/> representing the add or merge operations.</returns>
    /// <remarks>
    /// For each item, if an item with the same key exists in a previous layer, the two items are merged using <see cref="IIncrementalObject.ApplyChanges"/>.
    /// </remarks>
    public static IncrementalKeyedCollection<TKey, TValue> AddOrApplyChanges<TKey, TValue>( IEnumerable<TValue> items )
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new( items.ToImmutableDictionary( i => i.Key, i => new IncrementalKeyedCollection<TKey, TValue>.Item( i ) ) );

    /// <summary>
    /// Creates a new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the operation of removing an item
    /// from the collection.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of values in the collection.</typeparam>
    /// <param name="item">The item to remove from the collection (identified by its key).</param>
    /// <returns>An <see cref="IncrementalKeyedCollection{TKey,TValue}"/> representing the removal of the specified item.</returns>
    /// <remarks>
    /// If the item was previously added by another layer, this operation removes it from the final merged collection.
    /// </remarks>
    public static IncrementalKeyedCollection<TKey, TValue> Remove<TKey, TValue>( TValue item )
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new(
            ImmutableDictionary.Create<TKey, IncrementalKeyedCollection<TKey, TValue>.Item>()
                .Add( item.Key, new IncrementalKeyedCollection<TKey, TValue>.Item( item, false ) ) );

    /// <summary>
    /// Creates a new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the operation of removing items
    /// from the collection.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of values in the collection.</typeparam>
    /// <param name="items">The items to remove from the collection (identified by their keys).</param>
    /// <returns>An <see cref="IncrementalKeyedCollection{TKey,TValue}"/> representing the removal of the specified items.</returns>
    /// <remarks>
    /// If any items were previously added by another layer, this operation removes them from the final merged collection.
    /// </remarks>
    public static IncrementalKeyedCollection<TKey, TValue> Remove<TKey, TValue>( params TValue[] items )
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new( items.ToImmutableDictionary( i => i.Key, i => new IncrementalKeyedCollection<TKey, TValue>.Item( i, false ) ) );

    /// <summary>
    /// Creates a new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the operation of removing items
    /// from the collection.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the collection.</typeparam>
    /// <typeparam name="TValue">The type of values in the collection.</typeparam>
    /// <param name="items">The items to remove from the collection (identified by their keys).</param>
    /// <returns>An <see cref="IncrementalKeyedCollection{TKey,TValue}"/> representing the removal of the specified items.</returns>
    /// <remarks>
    /// If any items were previously added by another layer, this operation removes them from the final merged collection.
    /// </remarks>
    public static IncrementalKeyedCollection<TKey, TValue> Remove<TKey, TValue>( IEnumerable<TValue> items )
        where TValue : class, IIncrementalKeyedCollectionItem<TKey>
        where TKey : notnull
        => new( items.ToImmutableDictionary( i => i.Key, i => new IncrementalKeyedCollection<TKey, TValue>.Item( i, false ) ) );
}