// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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