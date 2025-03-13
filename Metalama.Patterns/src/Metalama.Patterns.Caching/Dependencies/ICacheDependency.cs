// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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