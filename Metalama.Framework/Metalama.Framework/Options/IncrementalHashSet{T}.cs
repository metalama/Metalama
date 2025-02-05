// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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
    /// additionally to any operation represented by the current collection.
    /// </summary>
    public IncrementalHashSet<T> Add( T item )
    {
        return this.Create( this._dictionary.SetItem( item, true ), this._clear );
    }

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of adding items to
    /// the collection, additionally to any operation represented by the current collection.
    /// </summary>
    public IncrementalHashSet<T> Add( T[] items ) => this.Add( (IEnumerable<T>) items );

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of adding items to
    /// the collection, additionally to any operation represented by the current collection.
    /// </summary>
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
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the option of removing an item
    /// from the collection, additionally to any operation represented by the current object.
    /// </summary>
    public IncrementalHashSet<T> Remove( T item ) => this.Create( this._dictionary.SetItem( item, false ), this._clear );

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the option of removing a items
    /// from the collection, additionally to any operation represented by the current object.
    /// </summary>
    public IncrementalHashSet<T> Remove( T[] items ) => this.Remove( (IEnumerable<T>) items );

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the option of removing a items
    /// from the , additionally to any operation represented by the current object.
    /// </summary>
    public IncrementalHashSet<T> Remove( IEnumerable<T> items )
    {
        var builder = this._dictionary.ToBuilder();

        foreach ( var item in items )
        {
            builder[item] = false;
        }

        return this.Create( builder.ToImmutable(), this._clear );
    }

    public IEnumerator<T> GetEnumerator() => this._dictionary.Where( x => x.Value ).Select( x => x.Key ).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

    /// <summary>
    /// Gets the number of items in the current collection.
    /// </summary>
    public int Count => this._count ??= this._dictionary.Count( x => x.Value );

    public bool IsEmpty => this.Count == 0;

    /// <summary>
    /// Overrides the current collection with another collection and returns the result.
    /// </summary>
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