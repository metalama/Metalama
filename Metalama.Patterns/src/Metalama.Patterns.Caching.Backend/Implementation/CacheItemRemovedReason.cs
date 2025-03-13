// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Enumerates the reasons why an item can be removed from the cache.
/// </summary>
public enum CacheItemRemovedReason
{
    /// <summary>
    /// Directly removed from the cache, by calling the <see cref="CachingBackend.RemoveItem(string)"/>, or an invalidation method that invalidates
    /// a cached method directly (not indirectly through dependencies).
    /// </summary>
    Removed,

    /// <summary>
    /// Removed because of <see cref="ICacheItemConfiguration.AbsoluteExpiration"/> or <see cref="ICacheItemConfiguration.SlidingExpiration"/>.
    /// </summary>
    Expired,

    /// <summary>
    /// Evicted to make space for newer cache items.
    /// </summary>
    Evicted,

    /// <summary>
    /// Indirectly invalidated (through dependencies).
    /// </summary>
    Invalidated,

    /// <summary>
    /// Other (or any reason that cannot be determined).
    /// </summary>
    Other
}