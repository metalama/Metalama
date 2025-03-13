// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Arguments of the <see cref="CachingBackend.ItemRemoved"/> event.
/// </summary>
[PublicAPI]
public sealed class CacheItemRemovedEventArgs : EventArgs
{
    internal CacheItemRemovedEventArgs( string key, CacheItemRemovedReason removedReason, Guid sourceId )
    {
        this.Key = key ?? throw new ArgumentNullException( nameof(key) );
        this.RemovedReason = removedReason;
        this.SourceId = sourceId;
    }

    /// <summary>
    /// Gets the key of the removed cached item.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the reason of the removal.
    /// </summary>
    public CacheItemRemovedReason RemovedReason { get; }

    /// <summary>
    /// Gets the <see cref="Guid"/> of the <see cref="CachingBackend"/> that caused the removal,
    /// or <see cref="Guid.Empty"/> if it cannot be determined or does not apply.
    /// </summary>
    public Guid SourceId { get; }
}