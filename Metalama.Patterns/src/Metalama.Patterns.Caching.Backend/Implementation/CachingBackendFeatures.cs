// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// List of features that can be implemented or not by a <see cref="CachingBackend"/>.
/// </summary>
[PublicAPI]
public class CachingBackendFeatures
{
    /// <summary>
    /// Gets a value indicating whether the <see cref="CachingBackend.Clear"/> method is supported.
    /// </summary>
    public virtual bool Clear => true;

    /// <summary>
    /// Gets a value indicating whether the <see cref="CachingBackend.ItemRemoved"/> and <see cref="CachingBackend.DependencyInvalidated"/> events are raised.
    /// </summary>
    public virtual bool Events => true;

    /// <summary>
    /// Gets a value indicating whether methods that modify the cache content run to completion before the control is given back to the calling method.
    /// If <c>false</c>, methods may run in the background, and the calling thread may not have a consistent view of the cache.
    /// </summary>
    public virtual bool Blocking => true;

    /// <summary>
    /// Gets a value indicating whether dependencies are supported.
    /// </summary>
    public virtual bool Dependencies => true;

    /// <summary>
    /// Gets a value indicating whether the <see cref="CachingBackend.ContainsDependency(string)"/> method is supported.
    /// </summary>
    public virtual bool ContainsDependency => true;
}