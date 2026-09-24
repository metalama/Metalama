// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using System.Collections.Immutable;

namespace Metalama.Patterns.Caching.Backends;

/// <summary>
/// A <see cref="CacheItem"/> as stored by <see cref="MemoryCachingBackend"/>. Do not use this type if you are not
/// implementing a <see cref="CachingBackend"/>.
/// </summary>
/// <param name="Value">The cached value, or its serialized representation when the backend has a serializer.</param>
/// <param name="Dependencies">The dependencies of the item.</param>
/// <param name="State">
/// An object that identifies one stored entry. <see cref="MemoryCachingBackend"/> replaces it with an object of its own
/// when it stores the item, and uses that object to record whether the entry has already been removed, replaced or
/// evicted. A caller can pass any object.
/// </param>
internal record MemoryCacheItem( object? Value, ImmutableArray<string> Dependencies, object State ) : CacheItem( Value, Dependencies );
