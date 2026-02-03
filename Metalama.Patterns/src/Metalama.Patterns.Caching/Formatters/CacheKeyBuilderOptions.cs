// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Formatters;

/// <summary>
/// Options of the <see cref="CacheKeyBuilder"/> class.
/// </summary>
[PublicAPI]
public record CacheKeyBuilderOptions
{
    /// <summary>
    /// Gets or sets the maximum size of the cache key before compression. Default is 1024.
    /// </summary>
    public int MaxKeySize { get; init; } = 1024;

    /// <summary>
    /// Gets or sets the hashing algorithm used to compress keys when their length exceeds <see cref="KeyCompressingThreshold"/>.
    /// Default is <see cref="CacheKeyHashingAlgorithm.None"/>.
    /// </summary>
    public CacheKeyHashingAlgorithm HashingAlgorithm { get; init; } = CacheKeyHashingAlgorithm.None;

    /// <summary>
    /// Gets or sets the length threshold above which a key will be compressed using <see cref="HashingAlgorithm"/>.
    /// Only effective when <see cref="HashingAlgorithm"/> is not <see cref="CacheKeyHashingAlgorithm.None"/>.
    /// Default is 0 (compress all keys when hashing is enabled).
    /// </summary>
    public int KeyCompressingThreshold { get; init; }
}