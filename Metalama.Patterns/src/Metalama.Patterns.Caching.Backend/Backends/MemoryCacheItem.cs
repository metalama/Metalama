// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Patterns.Caching.Implementation;
using System.Collections.Immutable;

namespace Metalama.Patterns.Caching.Backends;

/// <summary>
/// Meant to be used by caching backends. It's a <see cref="CacheItem"/> with an extra object that functions as a lock. Do not use this if you're not
/// implementing a <see cref="CachingBackend"/>.
/// </summary>
internal record MemoryCacheItem( object? Value, ImmutableArray<string> Dependencies, object Sync ) : CacheItem( Value, Dependencies );