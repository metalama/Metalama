// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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