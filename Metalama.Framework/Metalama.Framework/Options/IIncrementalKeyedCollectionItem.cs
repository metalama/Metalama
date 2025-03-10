// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Serialization;

namespace Metalama.Framework.Options;

/// <summary>
/// An item in a <see cref="IncrementalKeyedCollection{TKey,TValue}"/>.
/// </summary>
/// <seealso href="@exposing-options"/>
public interface IIncrementalKeyedCollectionItem<out TKey> : IIncrementalObject, ICompileTimeSerializable
    where TKey : notnull
{
    /// <summary>
    /// Gets the key that uniquely identifies the item in the collection.
    /// </summary>
    public TKey Key { get; }
}