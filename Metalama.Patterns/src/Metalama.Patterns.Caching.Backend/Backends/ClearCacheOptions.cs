// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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