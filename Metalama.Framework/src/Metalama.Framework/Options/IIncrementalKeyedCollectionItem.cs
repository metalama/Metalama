// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Serialization;

namespace Metalama.Framework.Options;

/// <summary>
/// An item in a <see cref="IncrementalKeyedCollection{TKey,TValue}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Classes that implement this interface can be used as values in an <see cref="IncrementalKeyedCollection{TKey,TValue}"/>.
/// Each item must provide a unique <see cref="Key"/> and implement the <see cref="IIncrementalObject.ApplyChanges"/> method
/// to support merging of items with the same key across different configuration layers.
/// </para>
/// <para>
/// Because this interface extends <see cref="IIncrementalObject"/>, items in the collection must be immutable and use the
/// same pattern as hierarchical options: properties should typically be nullable, and <see cref="IIncrementalObject.ApplyChanges"/>
/// should merge properties from two instances, with the <c>changes</c> parameter taking precedence for non-null values.
/// </para>
/// </remarks>
/// <typeparam name="TKey">The type of the key that identifies items in the collection.</typeparam>
/// <seealso cref="IncrementalKeyedCollection{TKey,TValue}"/>
/// <seealso cref="IIncrementalObject"/>
/// <seealso href="@exposing-options"/>
public interface IIncrementalKeyedCollectionItem<out TKey> : IIncrementalObject, ICompileTimeSerializable
    where TKey : notnull
{
    /// <summary>
    /// Gets the key that uniquely identifies the item in the collection.
    /// </summary>
    /// <value>
    /// The unique key for this item. Items with the same key will be merged using <see cref="IIncrementalObject.ApplyChanges"/>
    /// when combining collection layers.
    /// </value>
    TKey Key { get; }
}