// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Arguments of the <see cref="CachingBackend.DependencyInvalidated"/> event.
/// </summary>
[PublicAPI]
public sealed class CacheDependencyInvalidatedEventArgs : EventArgs
{
    /// <summary>
    /// Gets the key of the invalidated dependency.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the <see cref="Guid"/> of the <see cref="CachingBackend"/>
    /// instance that requested the invalidation, or <see cref="Guid.Empty"/>
    /// if this information is not available.
    /// </summary>
    public Guid SourceId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheDependencyInvalidatedEventArgs"/> class.
    /// </summary>
    /// <param name="key">The key of the invalidated dependency.</param>
    /// <param name="sourceId">The <see cref="Guid"/> of the <see cref="CachingBackend"/>
    /// instance that requested the invalidation, or <see cref="Guid.Empty"/>
    /// if this information is not available.</param>
    public CacheDependencyInvalidatedEventArgs( string key, Guid sourceId )
    {
        this.Key = key;
        this.SourceId = sourceId;
    }
}