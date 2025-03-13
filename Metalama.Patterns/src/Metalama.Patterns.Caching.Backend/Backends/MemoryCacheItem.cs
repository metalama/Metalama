// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;
using System.Collections.Immutable;

namespace Metalama.Patterns.Caching.Backends;

/// <summary>
/// Meant to be used by caching backends. It's a <see cref="CacheItem"/> with an extra object that functions as a lock. Do not use this if you're not
/// implementing a <see cref="CachingBackend"/>.
/// </summary>
internal record MemoryCacheItem( object? Value, ImmutableArray<string> Dependencies, object Sync ) : CacheItem( Value, Dependencies );