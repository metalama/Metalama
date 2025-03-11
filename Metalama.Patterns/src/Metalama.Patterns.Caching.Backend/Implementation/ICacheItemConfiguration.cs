// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Framework.Aspects;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Configuration of a cached method determined at compile time.
/// </summary>
[PublicAPI]
[RunTimeOrCompileTime]
public interface ICacheItemConfiguration
{
    /// <summary>
    /// Gets the total duration during which the result of the cached methods  is stored in cache. The absolute
    /// expiration time is counted from the moment the method is evaluated and cached.
    /// </summary>
    TimeSpan? AbsoluteExpiration { get; }

    /// <summary>
    /// Gets a value indicating whether the method calls are automatically reloaded (by re-evaluating the target method with the same arguments)
    /// when the cache item is removed from the cache.
    /// </summary>
    bool? AutoReload { get; }

    /// <summary>
    /// Gets the priority of the cached method.
    /// </summary>
    CacheItemPriority? Priority { get; }

    /// <summary>
    /// Gets the name of the caching profile that contains the configuration of the cached methods.
    /// </summary>
    string? ProfileName { get; }

    /// <summary>
    /// Gets the duration during which the result of the cached methods is stored in cache after it has been
    /// added to or accessed from the cache. The expiration is extended every time the value is accessed from the cache.
    /// </summary>
    TimeSpan? SlidingExpiration { get; }

    /// <summary>
    /// Gets a value indicating whether caching is enabled.
    /// </summary>
    bool? IsEnabled { get; }
}