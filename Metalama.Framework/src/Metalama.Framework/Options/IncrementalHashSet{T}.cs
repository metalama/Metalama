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
/// An immutable hash set where each class instance does not represent the full set but a modification of another set (possibly empty).
/// This class implements the <see cref="IIncrementalObject"/> interface and can be easily used in the context of an <see cref="IHierarchicalOptions{T}"/>.
/// The class can represent the <see cref="Add(T)"/>, <see cref="Remove(T)"/> and <see cref="IncrementalHashSet.Clear{T}"/> operations.
/// </summary>
/// <typeparam name="T">Type of items.</typeparam>
/// <remarks>
/// <para>
/// <see cref="IncrementalHashSet{T}"/> is designed for use as a property type within hierarchical options classes. Unlike a regular
/// hash set, each instance represents a <i>layer of changes</i> to a set rather than the complete set itself. When multiple layers
/// are merged using <see cref="ApplyChanges"/>, the operations from each layer are combined to produce the effective final set.
/// </para>
/// <para>
/// For example, if one layer adds items {"A", "B"} and another layer removes {"B"} and adds {"C"}, the final merged set contains {"A", "C"}.
/// The <see cref="IncrementalHashSet.Clear{T}"/> operation creates a layer that clears all previous items, starting fresh.
/// </para>
/// <para>
/// This class is immutable. All operations (<see cref="Add(T)"/>, <see cref="Remove(T)"/>, etc.) return new instances rather than
/// modifying the current instance.
/// </para>
/// </remarks>
/// <seealso cref="IIncrementalObject"/>
/// <seealso cref="IHierarchicalOptions"/>
/// <seealso cref="IncrementalKeyedCollection{TKey,TValue}"/>
/// <seealso href="@exposing-options"/>
[PublicAPI]
public partial class IncrementalHashSet<T> : IIncrementalObject, IReadOnlyCollection<T>, ICompileTimeSerializable
    where T : notnull
{
    /// <summary>
    /// Gets an <see cref="IncrementalHashSet{T}"/> that represents the absence of any change in the collection.
    /// </summary>
    /// <remarks>
    /// If you are looking for an object resulting in an empty collection even if the previous collection is not empty,
    /// use <see cref="IncrementalHashSet.Clear{T}"/>.
    /// </remarks>
    public static IncrementalHashSet<T> Empty { get; } = new( ImmutableDictionary<T, bool>.Empty );

    private readonly bool _clear;
    private ImmutableDictionary<T, bool> _dictionary;

    [NonCompileTimeSerialized]
    private int? _count;

    protected internal IncrementalHashSet( ImmutableDictionary<T, bool> dictionary, bool clear = false )
    {
        this._dictionary = dictionary;
        this._clear = clear;
    }

    protected virtual IncrementalHashSet<T> Create( ImmutableDictionary<T, bool> dictionary, bool clear = false ) => new( dictionary, clear );

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of adding an item to the collection,
    /// in addition to any operations represented by the current collection.
    /// </summary>
    /// <param name="item">The item to add to the collection.</param>
    /// <returns>A new <see cref="IncrementalHashSet{T}"/> with the additional add operation.</returns>
    /// <remarks>
    /// This method does not modify the current instance; it returns a new instance with the combined operations.
    /// </remarks>
    public IncrementalHashSet<T> Add( T item )
    {
        return this.Create( this._dictionary.SetItem( item, true ), this._clear );
    }

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of adding items to
    /// the collection, in addition to any operations represented by the current collection.
    /// </summary>
    /// <param name="items">The items to add to the collection.</param>
    /// <returns>A new <see cref="IncrementalHashSet{T}"/> with the additional add operations.</returns>
    /// <remarks>
    /// This method does not modify the current instance; it returns a new instance with the combined operations.
    /// </remarks>
    public IncrementalHashSet<T> Add( T[] items ) => this.Add( (IEnumerable<T>) items );

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of adding items to
    /// the collection, in addition to any operations represented by the current collection.
    /// </summary>
    /// <param name="items">The items to add to the collection.</param>
    /// <returns>A new <see cref="IncrementalHashSet{T}"/> with the additional add operations.</returns>
    /// <remarks>
    /// This method does not modify the current instance; it returns a new instance with the combined operations.
    /// </remarks>
    public IncrementalHashSet<T> Add( IEnumerable<T> items )
    {
        var builder = this._dictionary.ToBuilder();

        foreach ( var item in items )
        {
            builder[item] = true;
        }

        return this.Create( builder.ToImmutable(), this._clear );
    }

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of removing an item
    /// from the collection, in addition to any operations represented by the current object.
    /// </summary>
    /// <param name="item">The item to remove from the collection.</param>
    /// <returns>A new <see cref="IncrementalHashSet{T}"/> with the additional remove operation.</returns>
    /// <remarks>
    /// This method does not modify the current instance; it returns a new instance with the combined operations.
    /// </remarks>
    public IncrementalHashSet<T> Remove( T item ) => this.Create( this._dictionary.SetItem( item, false ), this._clear );

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of removing items
    /// from the collection, in addition to any operations represented by the current object.
    /// </summary>
    /// <param name="items">The items to remove from the collection.</param>
    /// <returns>A new <see cref="IncrementalHashSet{T}"/> with the additional remove operations.</returns>
    /// <remarks>
    /// This method does not modify the current instance; it returns a new instance with the combined operations.
    /// </remarks>
    public IncrementalHashSet<T> Remove( T[] items ) => this.Remove( (IEnumerable<T>) items );

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of removing items
    /// from the collection, in addition to any operations represented by the current object.
    /// </summary>
    /// <param name="items">The items to remove from the collection.</param>
    /// <returns>A new <see cref="IncrementalHashSet{T}"/> with the additional remove operations.</returns>
    /// <remarks>
    /// This method does not modify the current instance; it returns a new instance with the combined operations.
    /// </remarks>
    public IncrementalHashSet<T> Remove( IEnumerable<T> items )
    {
        var builder = this._dictionary.ToBuilder();

        foreach ( var item in items )
        {
            builder[item] = false;
        }

        return this.Create( builder.ToImmutable(), this._clear );
    }

    /// <summary>
    /// Returns an enumerator that iterates through the items in the collection.
    /// </summary>
    /// <returns>An enumerator for the items that are currently in the set (considering add and remove operations).</returns>
    public IEnumerator<T> GetEnumerator() => this._dictionary.Where( x => x.Value ).Select( x => x.Key ).GetEnumerator();

    /// <summary>
    /// Returns an enumerator that iterates through the items in the collection.
    /// </summary>
    /// <returns>An enumerator for the items that are currently in the set.</returns>
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <summary>
    /// Gets the number of items in the current collection layer.
    /// </summary>
    /// <value>
    /// The count of items that are marked as added (not removed) in this incremental hash set layer.
    /// </value>
    public int Count => this._count ??= this._dictionary.Count( x => x.Value );

    /// <summary>
    /// Gets a value indicating whether this collection layer is empty.
    /// </summary>
    /// <value>
    /// <c>true</c> if the collection layer has no items marked as added; otherwise, <c>false</c>.
    /// </value>
    public bool IsEmpty => this.Count == 0;

    /// <summary>
    /// Merges the current collection layer with another collection layer and returns the result.
    /// </summary>
    /// <param name="other">The collection layer to merge with the current layer.</param>
    /// <param name="context">The context of the merge operation.</param>
    /// <returns>A new <see cref="IncrementalHashSet{T}"/> representing the merged result.</returns>
    /// <remarks>
    /// If <paramref name="other"/> contains a clear operation, it takes precedence and the result contains only the operations from <paramref name="other"/>.
    /// Otherwise, operations from both layers are combined. For any item that appears in both layers, the operation from <paramref name="other"/>
    /// takes precedence (i.e., if the current layer adds an item and <paramref name="other"/> removes it, the final result will not contain the item).
    /// </remarks>
    public IncrementalHashSet<T> ApplyChanges( IncrementalHashSet<T> other, in ApplyChangesContext context )
    {
        if ( other._clear )
        {
            return other;
        }
        else
        {
            var items = this._dictionary;

            foreach ( var pair in other._dictionary )
            {
                items = items.SetItem( pair.Key, pair.Value );
            }

            return this.Create( items );
        }
    }

    object IIncrementalObject.ApplyChanges( object changes, in ApplyChangesContext context ) => this.ApplyChanges( (IncrementalHashSet<T>) changes, context );
}