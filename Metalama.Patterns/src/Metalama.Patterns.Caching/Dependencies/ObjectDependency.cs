// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Dependencies;

/// <summary>
/// Wraps an <see cref="object"/> into an <see cref="ObjectDependency"/>. The <see cref="GetCacheKey"/> method
/// relies on the <see cref="CachingService.KeyBuilder"/> to create the cache key of the wrapped object.
/// </summary>
[PublicAPI]
public sealed record ObjectDependency( object Object ) : ICacheDependency
{
    /// <param name="cachingService"></param>
    /// <inheritdoc />
    public string GetCacheKey( ICachingService cachingService ) => cachingService.KeyBuilder.BuildDependencyKey( this.Object );

    IReadOnlyCollection<ICacheDependency> ICacheDependency.CascadeDependencies => Array.Empty<ICacheDependency>();
}