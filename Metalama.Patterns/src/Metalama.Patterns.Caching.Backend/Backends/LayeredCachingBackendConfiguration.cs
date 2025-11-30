// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Backends;

/// <summary>
/// Configuration for a layered caching backend that combines a local (L1) in-memory cache with a remote (L2) cache.
/// </summary>
/// <seealso cref="CachingBackendConfiguration"/>
/// <seealso cref="MemoryCachingBackendConfiguration"/>
/// <seealso cref="Building.LayeredCachingBackendBuilder"/>
public sealed record LayeredCachingBackendConfiguration : CachingBackendConfiguration
{
    /// <summary>
    /// Gets or sets the configuration for the L1 (local in-memory) cache layer.
    /// </summary>
    public MemoryCachingBackendConfiguration? L1Configuration { get; init; }
}