// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Dependencies;

/// <summary>
/// Wraps an <see cref="object"/> into an <see cref="ICacheDependency"/>. The <see cref="ICacheDependency.GetCacheKey"/> method
/// relies on the <see cref="CachingService.KeyBuilder"/> to create the cache key of the wrapped object.
/// </summary>
/// <remarks>
/// <para>Use this class when you have objects that already implement cache key generation (through
/// the <c>CacheKeyAttribute</c> aspect or a custom formatter) and want to use them as dependencies.</para>
/// <para>Alternatively, you can use the <see cref="CachingServiceExtensions.AddObjectDependency"/> and
/// <see cref="CachingServiceExtensions.InvalidateObject"/> extension methods to avoid creating wrapper objects.</para>
/// </remarks>
/// <param name="Object">The object to wrap as a cache dependency.</param>
/// <seealso cref="ICacheDependency"/>
/// <seealso cref="StringDependency"/>
/// <seealso cref="CachingServiceExtensions.AddObjectDependency"/>
/// <seealso cref="CachingServiceExtensions.InvalidateObject"/>
/// <seealso href="@caching-dependencies"/>
[PublicAPI]
public sealed record ObjectDependency( object Object ) : ICacheDependency
{
    /// <param name="cachingService"></param>
    /// <inheritdoc />
    public string GetCacheKey( ICachingService cachingService ) => cachingService.KeyBuilder.BuildDependencyKey( this.Object );

    IReadOnlyCollection<ICacheDependency> ICacheDependency.CascadeDependencies => Array.Empty<ICacheDependency>();
}