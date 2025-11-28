// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Options;

#pragma warning disable SA1642

/// <summary>
/// Factory methods for the <see cref="IncrementalHashSet{T}"/> generic class.
/// </summary>
/// <remarks>
/// <para>
/// This static class provides convenient factory methods for creating <see cref="IncrementalHashSet{T}"/> instances.
/// These methods are useful when constructing incremental hash sets as properties within hierarchical options classes.
/// </para>
/// <para>
/// Incremental hash sets are typically used in option classes when you need a property that represents a set of items
/// that can be added to or removed from across different configuration layers. For example, you might have a set of
/// excluded method names that can be extended or reduced at the project, namespace, or type level.
/// </para>
/// </remarks>
/// <seealso cref="IncrementalHashSet{T}"/>
/// <seealso cref="IHierarchicalOptions"/>
/// <seealso href="@exposing-options"/>
[CompileTime]
public static class IncrementalHashSet
{
    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the absence of any operation.
    /// </summary>
    /// <typeparam name="T">The type of items in the hash set.</typeparam>
    /// <returns>An empty <see cref="IncrementalHashSet{T}"/> representing no changes to the collection.</returns>
    /// <remarks>
    /// This is equivalent to not setting the property at all. When merged with another set, the other set's operations take precedence.
    /// </remarks>
    public static IncrementalHashSet<T> Empty<T>()
        where T : notnull
        => IncrementalHashSet<T>.Empty;

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of clearing
    /// the collection of all items.
    /// </summary>
    /// <typeparam name="T">The type of items in the hash set.</typeparam>
    /// <returns>An <see cref="IncrementalHashSet{T}"/> that, when merged, clears all previous items from the collection.</returns>
    /// <remarks>
    /// This operation discards all items from previous layers. It is useful when you want to start with an empty set
    /// at a specific configuration level, ignoring all inherited items.
    /// </remarks>
    public static IncrementalHashSet<T> Clear<T>()
        where T : notnull
        => new( ImmutableDictionary<T, bool>.Empty, true );

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of adding an item to
    /// the collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the hash set.</typeparam>
    /// <param name="item">The item to add to the collection.</param>
    /// <returns>An <see cref="IncrementalHashSet{T}"/> representing the addition of the specified item.</returns>
    /// <remarks>
    /// If the item was previously removed by another layer, this operation re-adds it to the final merged collection.
    /// </remarks>
    public static IncrementalHashSet<T> Add<T>( T item )
        where T : notnull
        => new(
            ImmutableDictionary.Create<T, bool>()
                .Add( item, true ) );

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of adding items to
    /// the collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the hash set.</typeparam>
    /// <param name="items">The items to add to the collection.</param>
    /// <returns>An <see cref="IncrementalHashSet{T}"/> representing the addition of the specified items.</returns>
    /// <remarks>
    /// If any items were previously removed by another layer, this operation re-adds them to the final merged collection.
    /// </remarks>
    public static IncrementalHashSet<T> Add<T>( params T[] items )
        where T : notnull
        => new( items.ToImmutableDictionary( i => i, _ => true ) );

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of adding items to
    /// the collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the hash set.</typeparam>
    /// <param name="items">The items to add to the collection.</param>
    /// <returns>An <see cref="IncrementalHashSet{T}"/> representing the addition of the specified items.</returns>
    /// <remarks>
    /// If any items were previously removed by another layer, this operation re-adds them to the final merged collection.
    /// </remarks>
    public static IncrementalHashSet<T> Add<T>( IEnumerable<T> items )
        where T : notnull
        => new( items.ToImmutableDictionary( i => i, _ => true ) );

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of removing an item
    /// from the collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the hash set.</typeparam>
    /// <param name="item">The item to remove from the collection.</param>
    /// <returns>An <see cref="IncrementalHashSet{T}"/> representing the removal of the specified item.</returns>
    /// <remarks>
    /// If the item was previously added by another layer, this operation removes it from the final merged collection.
    /// </remarks>
    public static IncrementalHashSet<T> Remove<T>( T item )
        where T : notnull
        => new(
            ImmutableDictionary.Create<T, bool>()
                .Add( item, false ) );

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of removing items
    /// from the collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the hash set.</typeparam>
    /// <param name="items">The items to remove from the collection.</param>
    /// <returns>An <see cref="IncrementalHashSet{T}"/> representing the removal of the specified items.</returns>
    /// <remarks>
    /// If any items were previously added by another layer, this operation removes them from the final merged collection.
    /// </remarks>
    public static IncrementalHashSet<T> Remove<T>( params T[] items )
        where T : notnull
        => new( items.ToImmutableDictionary( i => i, _ => false ) );

    /// <summary>
    /// Creates a new <see cref="IncrementalHashSet{T}"/> that represents the operation of removing items
    /// from the collection.
    /// </summary>
    /// <typeparam name="T">The type of items in the hash set.</typeparam>
    /// <param name="items">The items to remove from the collection.</param>
    /// <returns>An <see cref="IncrementalHashSet{T}"/> representing the removal of the specified items.</returns>
    /// <remarks>
    /// If any items were previously added by another layer, this operation removes them from the final merged collection.
    /// </remarks>
    public static IncrementalHashSet<T> Remove<T>( IEnumerable<T> items )
        where T : notnull
        => new( items.ToImmutableDictionary( i => i, _ => false ) );
}