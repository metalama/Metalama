// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Microsoft.Extensions.Caching.Memory;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// An <see cref="IMemoryCache"/> that can remove all of its entries.
/// </summary>
/// <remarks>
/// <see cref="Microsoft.Extensions.Caching.Memory.MemoryCache"/> exposes these two operations, but
/// <see cref="IMemoryCache"/> does not declare them. An implementation of <see cref="IMemoryCache"/> that also
/// implements this interface supports the <c>Clear</c> feature of
/// <see cref="Metalama.Patterns.Caching.Backends.MemoryCachingBackend"/>.
/// </remarks>
public interface IClearableMemoryCache : IMemoryCache
{
    /// <summary>
    /// Removes all entries.
    /// </summary>
    void Clear();

    /// <summary>
    /// Removes the given percentage of the entries.
    /// </summary>
    /// <param name="percentage">The percentage of entries to remove, between 0 and 1.</param>
    void Compact( double percentage );
}
