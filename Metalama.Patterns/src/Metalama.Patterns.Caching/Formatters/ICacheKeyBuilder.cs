// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

namespace Metalama.Patterns.Caching.Formatters;

/// <summary>
/// Builds cache item keys and dependency keys.
/// </summary>
public interface ICacheKeyBuilder
{
    /// <summary>
    /// Builds a cache key for a given method call.
    /// </summary>
    /// <param name="metadata">The <see cref="CachedMethodMetadata"/> representing the method.</param>
    /// <param name="instance">The <c>this</c> instance of the method call, or <c>null</c> if the method is static.</param>
    /// <param name="arguments">The arguments passed to the method call.</param>
    /// <returns>A string uniquely representing the method call.</returns>
    string BuildMethodKey( CachedMethodMetadata metadata, object? instance, IList<object?> arguments );

    /// <summary>
    /// Builds a dependency key for a given object.
    /// </summary>
    /// <param name="o">An object.</param>
    /// <returns>A dependency key that uniquely represents <paramref name="o"/>.</returns>
    string BuildDependencyKey( object o );
}