// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Services;

namespace Metalama.Framework.Engine.Utilities.Caching;

/// <summary>
/// An optional observer interface for monitoring <see cref="WeakCache{TKey,TValue}"/> operations.
/// Used primarily for testing and diagnostics.
/// </summary>
[UsedImplicitly( ImplicitUseTargetFlags.Members )]
public interface IWeakCacheObserver : IGlobalService
{
    /// <summary>
    /// Called when a cache lookup results in a hit (the value was found in the cache).
    /// </summary>
    /// <param name="cacheName">The name of the cache.</param>
    void OnCacheHit( string cacheName );

    /// <summary>
    /// Called when a cache lookup results in a miss (the value was not found and had to be created).
    /// </summary>
    /// <param name="cacheName">The name of the cache.</param>
    void OnCacheMiss( string cacheName );
}
