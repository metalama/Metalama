// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;

namespace Metalama.Patterns.Caching.Dependencies;

/// <summary>
/// A cache dependency that is already represented as a string.
/// </summary>
/// <remarks>
/// <para>Use this class to represent singleton or global dependencies that don't have a corresponding object
/// (for example, "ProductList" or "PriceList" dependencies that represent entire collections).</para>
/// <para>For object-based dependencies, consider using <see cref="ObjectDependency"/> instead.</para>
/// </remarks>
/// <param name="Key">The string key that uniquely identifies this dependency.</param>
/// <seealso cref="ICacheDependency"/>
/// <seealso cref="ObjectDependency"/>
/// <seealso href="@caching-dependencies"/>
[PublicAPI]
public sealed record StringDependency( string Key ) : ICacheDependency
{
    /// <param name="cachingService"></param>
    /// <inheritdoc />
    string ICacheDependency.GetCacheKey( ICachingService cachingService ) => this.Key;

    IReadOnlyCollection<ICacheDependency> ICacheDependency.CascadeDependencies => Array.Empty<ICacheDependency>();
}