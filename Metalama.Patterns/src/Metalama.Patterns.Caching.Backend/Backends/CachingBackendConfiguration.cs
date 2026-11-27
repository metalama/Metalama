// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Backends;

/// <summary>
/// Base configuration record for <see cref="CachingBackend"/> implementations.
/// </summary>
/// <seealso cref="CachingBackend"/>
/// <seealso cref="MemoryCachingBackendConfiguration"/>
[PublicAPI]
public record CachingBackendConfiguration
{
    /// <summary>
    /// Gets a value indicating whether this backend is behind an L1 (local) cache in a layered configuration.
    /// </summary>
    public bool IsBehindL1 { get; internal init; }

    /// <summary>
    /// Gets or sets a debug name for this backend, used for logging and diagnostics.
    /// </summary>
    public string? DebugName { get; set; }

    /// <summary>
    /// Gets the duration for which the backend records the time of an invalidation, so that
    /// <see cref="CachingBackend.SetItemIfNotInvalidated"/> does not store a value whose computation started before the
    /// invalidation. The value of a computation that lasts longer than this duration is stored without this check. The
    /// default value is five minutes. This property is used only by the backends that support
    /// <see cref="Implementation.CachingBackendFeatures.SetItemIfNotInvalidated"/>.
    /// </summary>
    public TimeSpan InvalidationTombstoneLifetime { get; init; } = TimeSpan.FromMinutes( 5 );
}