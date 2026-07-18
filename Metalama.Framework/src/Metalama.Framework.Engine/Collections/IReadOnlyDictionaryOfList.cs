// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Collections
{
    /// <summary>
    /// A read-only view over a dictionary that maps each key to a list of values. Use this instead of
    /// <see cref="ImmutableDictionaryOfArray{TKey,TValue}"/> when the map is built once and thereafter only read,
    /// i.e. when the persistent-update capability of the immutable variant (its builder, and its cheap
    /// <c>Add</c>/<c>Merge</c> that share structure between versions) is not needed.
    /// </summary>
    /// <remarks>
    /// Note that this enumerates as <see cref="KeyValuePair{TKey,TValue}"/> of key and list, whereas
    /// <see cref="ImmutableDictionaryOfArray{TKey,TValue}"/> enumerates as
    /// <see cref="System.Linq.IGrouping{TKey,TElement}"/>.
    /// </remarks>
    [PublicAPI]
    public interface IReadOnlyDictionaryOfList<TKey, TValue> : IReadOnlyDictionary<TKey, IReadOnlyList<TValue>>
        where TKey : notnull
    {
        /// <summary>
        /// Gets the values for the given key, or an empty list if the key is absent.
        /// </summary>
        /// <remarks>
        /// This <b>deliberately deviates</b> from the <see cref="IReadOnlyDictionary{TKey,TValue}"/> contract, which
        /// specifies that the indexer throws <see cref="KeyNotFoundException"/> for an absent key. Returning empty
        /// matches <see cref="ImmutableDictionaryOfArray{TKey,TValue}"/>, whose call sites overwhelmingly treat "no
        /// values for this key" as a routine outcome rather than an error. Code that must distinguish an absent key
        /// from a key with no values should use <see cref="IReadOnlyDictionary{TKey,TValue}.ContainsKey"/> or
        /// <see cref="IReadOnlyDictionary{TKey,TValue}.TryGetValue"/>.
        /// </remarks>
        new IReadOnlyList<TValue> this[ TKey key ] { get; }
    }
}
