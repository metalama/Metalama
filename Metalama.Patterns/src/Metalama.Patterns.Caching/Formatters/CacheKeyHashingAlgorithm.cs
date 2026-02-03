// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Formatters;

/// <summary>
/// Enumerates the hashing algorithms that can be used with <see cref="CacheKeyBuilder"/> for key compression.
/// </summary>
[PublicAPI]
public enum CacheKeyHashingAlgorithm
{
    /// <summary>
    /// Does not hash the cache key regardless of its length.
    /// </summary>
    None,

    /// <summary>
    /// Uses <see cref="System.IO.Hashing.XxHash64"/>.
    /// </summary>
    XxHash64,

    /// <summary>
    /// Uses <see cref="System.IO.Hashing.XxHash128"/>.
    /// </summary>
    XxHash128
}