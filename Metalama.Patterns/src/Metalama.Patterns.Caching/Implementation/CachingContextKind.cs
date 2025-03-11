// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Enumerates the kinds of <see cref="ICachingContext"/>.
/// </summary>
[Flags]
internal enum CachingContextKind
{
    /// <summary>
    /// None (a null implementation of <see cref="ICachingContext"/>).
    /// </summary>
    None,

    /// <summary>
    /// The <see cref="ICachingContext"/> of a method being evaluated and added to the cache.
    /// </summary>
    Cache = 1,

    /// <summary>
    /// The <see cref="ICachingContext"/> of a method being re-evaluated, ignoring the previous value, and replaced into the cache, using the
    /// <see cref="CachingServiceExtensions.Refresh{TReturn}"/> method.
    /// </summary>
    Refresh = 2
}