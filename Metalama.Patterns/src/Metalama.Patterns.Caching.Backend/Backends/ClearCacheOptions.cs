// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Building;

namespace Metalama.Patterns.Caching.Backends;

/// <summary>
/// Options for the <see cref="CachingBackend.Clear"/> method.
/// </summary>
[Flags]
public enum ClearCacheOptions
{
    /// <summary>
    /// Clears all cache layers. Does not guarantee to raise the <see cref="CachingBackend.ItemRemoved"/> event.
    /// </summary>
    Default,

    /// <summary>
    /// Clears only the local cache, but does not attempt to clear the remote cache.
    /// (for use with <see cref="CachingBackendFactory.WithL1(Metalama.Patterns.Caching.Building.OutOfProcessCachingBackendBuilder,Metalama.Patterns.Caching.Backends.LayeredCachingBackendConfiguration?)"/>.
    /// </summary>
    Local = 1,

    /// <summary>
    /// Uses the <c>MemoryCache.Compact()</c> method. Raises the <see cref="CachingBackend.ItemRemoved"/> event, but does not guarantee that the cache
    /// is 100% free after the operation. 
    /// </summary>
    Compact = 2
}