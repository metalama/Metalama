// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Flashtrace.Formatters;

namespace Metalama.Patterns.Caching.Dependencies;

/// <summary>
/// Interface that must be implemented by classes that need to be used as cache dependencies,
/// for use with the <see cref="CachingServiceExtensions.AddDependency(ICachingService,ICacheDependency)"/> method.
/// Alternatively, custom classes may implement the <see cref="IFormattable{T}"/> interface or simply
/// the <see cref="object.ToString"/> method.
/// </summary>
public interface ICacheDependency
{
    /// <summary>
    /// Gets a string that uniquely represents the current object.
    /// </summary>
    /// <param name="cachingService"></param>
    /// <returns>A string that uniquely represents the current object.</returns>
    /// <remarks>
    /// <para>The returned key should be globally unique, not just unique within the class implementing the method.</para>
    /// </remarks>
    string GetCacheKey( ICachingService cachingService );

    /// <summary>
    /// Gets the list of dependencies that must also be invalidated when the current dependency is invalidated.
    /// </summary>
    /// <remarks>
    /// The implementation of this method should not perform I/O operations.
    /// </remarks>
    IReadOnlyCollection<ICacheDependency> CascadeDependencies
#if NET5_0_OR_GREATER
        => Array.Empty<ICacheDependency>();
#else
    {
        get;
    }
#endif
}