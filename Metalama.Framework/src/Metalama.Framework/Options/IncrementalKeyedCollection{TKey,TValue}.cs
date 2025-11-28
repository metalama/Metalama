// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Serialization;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Options;

/// <summary>
/// An immutable keyed collection where each class instance does not represent the full collection but a modification of another collection (possibly empty).
/// This class implements the <see cref="IIncrementalObject"/> interface and can be easily used in the context of an <see cref="IHierarchicalOptions{T}"/>.
/// The class can represent the <see cref="AddOrApplyChanges(TValue)"/>, <see cref="Remove(TKey)"/> and <see cref="IncrementalKeyedCollection.Clear{TKey,TValue}"/> operations.
/// </summary>
/// <typeparam name="TKey">Type of keys.</typeparam>
/// <typeparam name="TValue">Type of items, implementing the <see cref="IIncrementalKeyedCollectionItem{TKey}"/> interface.</typeparam>
/// <remarks>
/// <para>
/// <see cref="IncrementalKeyedCollection{TKey,TValue}"/> is designed for use as a property type within hierarchical options classes when
/// you need to work with collections of complex, keyed items. Unlike a regular dictionary or keyed collection, each instance represents
/// a <i>layer of changes</i> rather than the complete collection. When multiple layers are merged using <see cref="ApplyChanges"/>,
/// the operations from each layer are combined intelligently.
/// </para>
/// <para>
/// The key feature of this collection is that when an item with the same key exists in both the base collection and the overriding collection,
/// the two items are themselves merged using <see cref="IIncrementalObject.ApplyChanges"/>. This allows for fine-grained incremental
/// modifications to complex configuration objects.
/// </para>
/// <para>
/// For example, if one layer has a rule with key "DefaultPolicy" specifying cache duration of 60 seconds, and another layer provides a rule
/// with the same key specifying a different setting, the two rules are merged, allowing partial updates rather than complete replacement.
/// </para>
/// <para>
/// This class is immutable. All operations return new instances rather than modifying the current instance.
/// </para>
/// </remarks>
/// <seealso cref="IIncrementalObject"/>
/// <seealso cref="IHierarchicalOptions"/>
/// <seealso cref="IncrementalHashSet{T}"/>
/// <seealso cref="IIncrementalKeyedCollectionItem{TKey}"/>
/// <seealso href="@exposing-options"/>
[PublicAPI]
public partial class IncrementalKeyedCollection<TKey, TValue> : IIncrementalObject, IReadOnlyCollection<TValue>, ICompileTimeSerializable
    where TKey : notnull
    where TValue : class, IIncrementalKeyedCollectionItem<TKey>
{
    /// <summary>
    /// Gets an <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that represents the absence of any change in the collection.
    /// </summary>
    /// <remarks>
    /// If you are looking for an object resulting in an empty collection even if the previous collection is not empty,
    /// use <see cref="IncrementalKeyedCollection.Clear{TKey,TValue}"/>.
    /// </remarks>
    public static IncrementalKeyedCollection<TKey, TValue> Empty { get; } = new( ImmutableDictionary<TKey, Item>.Empty );

    private readonly bool _clear;
    private ImmutableDictionary<TKey, Item> _dictionary;

    [NonCompileTimeSerialized]
    private int? _count;

    protected internal IncrementalKeyedCollection( ImmutableDictionary<TKey, Item> dictionary, bool clear = false )
    {
        this._dictionary = dictionary;
        this._clear = clear;
    }

    protected virtual IncrementalKeyedCollection<TKey, TValue> Create( ImmutableDictionary<TKey, Item> items, bool clear = false ) => new( items, clear );

    /// <summary>
    /// Creates a <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that contains all operations already contained in the current object,
    /// plus the operation of adding an item, or merging it with an existing item if the same key already exists.
    /// </summary>
    /// <param name="item">The item to add or merge.</param>
    /// <returns>A new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> with the additional operation.</returns>
    /// <remarks>
    /// This method does not modify the current instance; it returns a new instance with the combined operations.
    /// If an item with the same key exists in the current layer, the items are merged using <see cref="IIncrementalObject.ApplyChanges"/>.
    /// </remarks>
    public IncrementalKeyedCollection<TKey, TValue> AddOrApplyChanges( TValue item )
    {
        var key = item.Key;

        TValue mergedItem;

        if ( this._dictionary.TryGetValue( key, out var oldItem ) && oldItem.IsEnabled )
        {
            mergedItem = oldItem.Value.ApplyChangesSafe( item, default )!;
        }
        else
        {
            mergedItem = item;
        }

        return this.Create( this._dictionary.SetItem( key, new Item( mergedItem ) ) );
    }

    /// <summary>
    /// Creates a <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that contains all operations already contained in the current object,
    /// plus the operation of adding items, or merging them with existing items if the same keys already exist.
    /// </summary>
    /// <param name="items">The items to add or merge.</param>
    /// <returns>A new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> with the additional operations.</returns>
    /// <remarks>
    /// This method does not modify the current instance; it returns a new instance with the combined operations.
    /// For each item, if an item with the same key exists in the current layer, the items are merged using <see cref="IIncrementalObject.ApplyChanges"/>.
    /// </remarks>
    public IncrementalKeyedCollection<TKey, TValue> AddOrApplyChanges( params TValue[] items ) => this.AddOrApplyChanges( (IEnumerable<TValue>) items );

    /// <summary>
    /// Creates a <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that contains all operations already contained in the current object,
    /// plus the operation of adding items, or merging them with existing items if the same keys already exist.
    /// </summary>
    /// <param name="items">The items to add or merge.</param>
    /// <returns>A new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> with the additional operations.</returns>
    /// <remarks>
    /// This method does not modify the current instance; it returns a new instance with the combined operations.
    /// For each item, if an item with the same key exists in the current layer, the items are merged using <see cref="IIncrementalObject.ApplyChanges"/>.
    /// </remarks>
    public IncrementalKeyedCollection<TKey, TValue> AddOrApplyChanges( IEnumerable<TValue> items )
    {
        var builder = this._dictionary.ToBuilder();

        foreach ( var item in items )
        {
            var key = item.Key;

            TValue mergedItem;

            if ( this._dictionary.TryGetValue( key, out var oldItem ) && oldItem.IsEnabled )
            {
                mergedItem = oldItem.Value.ApplyChangesSafe( item, default )!;
            }
            else
            {
                mergedItem = item;
            }

            builder[item.Key] = new Item( mergedItem );
        }

        return this.Create( builder.ToImmutable(), this._clear );
    }

    /// <summary>
    /// Creates a <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that contains all operations already contained in the current object,
    /// plus the operation of removing an item from the collection.
    /// </summary>
    /// <param name="key">The key of the item to remove.</param>
    /// <returns>A new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> with the additional remove operation.</returns>
    /// <remarks>
    /// This method does not modify the current instance; it returns a new instance with the combined operations.
    /// </remarks>
    public IncrementalKeyedCollection<TKey, TValue> Remove( TKey key ) => this.Create( this._dictionary.SetItem( key, new Item( default, false ) ) );

    /// <summary>
    /// Creates a <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that contains all operations already contained in the current object,
    /// plus the operation of removing items from the collection.
    /// </summary>
    /// <param name="keys">The keys of the items to remove.</param>
    /// <returns>A new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> with the additional remove operations.</returns>
    /// <remarks>
    /// This method does not modify the current instance; it returns a new instance with the combined operations.
    /// </remarks>
    public IncrementalKeyedCollection<TKey, TValue> Remove( TKey[] keys ) => this.Remove( (IEnumerable<TKey>) keys );

    /// <summary>
    /// Creates a <see cref="IncrementalKeyedCollection{TKey,TValue}"/> that contains all operations already contained in the current object,
    /// plus the operation of removing items from the collection.
    /// </summary>
    /// <param name="keys">The keys of the items to remove.</param>
    /// <returns>A new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> with the additional remove operations.</returns>
    /// <remarks>
    /// This method does not modify the current instance; it returns a new instance with the combined operations.
    /// </remarks>
    public IncrementalKeyedCollection<TKey, TValue> Remove( IEnumerable<TKey> keys )
    {
        var builder = this._dictionary.ToBuilder();

        foreach ( var key in keys )
        {
            if ( !(builder.TryGetValue( key, out var item ) && !item.IsEnabled) )
            {
                builder[key] = new Item( default, false );
            }
        }

        return this.Create( builder.ToImmutable(), this._clear );
    }

    /// <summary>
    /// Returns an enumerator that iterates through the items in the collection.
    /// </summary>
    /// <returns>An enumerator for the items that are currently enabled in the collection layer.</returns>
    public IEnumerator<TValue> GetEnumerator() => this._dictionary.Values.Where( i => i.IsEnabled ).Select( i => i.Value! ).GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the items in the collection.
    /// </summary>
    /// <returns>An enumerator for the items that are currently enabled in the collection.</returns>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <summary>
    /// Gets the number of items in the current collection layer.
    /// </summary>
    /// <value>
    /// The count of items that are enabled (not removed) in this incremental keyed collection layer.
    /// </value>
    public int Count => this._count ??= this._dictionary.Count( i => i.Value.IsEnabled );

    /// <summary>
    /// Gets a value indicating whether this collection layer is empty.
    /// </summary>
    /// <value>
    /// <c>true</c> if the collection layer has no enabled items; otherwise, <c>false</c>.
    /// </value>
    public bool IsEmpty => this.Count == 0;

    /// <summary>
    /// Merges the current collection layer with another collection layer and returns the result.
    /// </summary>
    /// <param name="overridingOptions">The collection layer to merge with the current layer.</param>
    /// <param name="context">The context of the merge operation.</param>
    /// <returns>A new <see cref="IncrementalKeyedCollection{TKey,TValue}"/> representing the merged result.</returns>
    /// <remarks>
    /// If <paramref name="overridingOptions"/> contains a clear operation, it takes precedence and the result contains only the operations from <paramref name="overridingOptions"/>.
    /// Otherwise, items from both layers are combined. When an item with the same key exists in both layers and both are enabled (not marked for removal),
    /// the two item instances are themselves merged using <see cref="IIncrementalObject.ApplyChanges"/>. In all other cases (removal operations,
    /// or item present in only one layer), the operation from <paramref name="overridingOptions"/> takes precedence if it defines that key.
    /// </remarks>
    public IncrementalKeyedCollection<TKey, TValue> ApplyChanges( IncrementalKeyedCollection<TKey, TValue> overridingOptions, in ApplyChangesContext context )
    {
        var dictionary = overridingOptions._clear ? ImmutableDictionary<TKey, Item>.Empty : this._dictionary;

        foreach ( var item in overridingOptions._dictionary )
        {
            if ( item.Value.IsEnabled && dictionary.TryGetValue( item.Key, out var existingItem ) && existingItem.IsEnabled )
            {
                // If we replace an enabled value by another enabled value, we have to merge the items.
                var newValue = (TValue) existingItem.Value!.ApplyChanges( item.Value.Value!, context );
                dictionary = dictionary.SetItem( item.Key, new Item( newValue ) );
            }
            else
            {
                // In all other cases, the new item wins.
                dictionary = dictionary.SetItem( item.Key, item.Value );
            }
        }

        return this.Create( dictionary );
    }

    object IIncrementalObject.ApplyChanges( object changes, in ApplyChangesContext context )
        => this.ApplyChanges( (IncrementalKeyedCollection<TKey, TValue>) changes, context );
}