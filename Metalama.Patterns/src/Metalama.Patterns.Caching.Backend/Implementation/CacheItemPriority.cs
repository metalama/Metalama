// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Patterns.Caching.Backends;

namespace Metalama.Patterns.Caching.Implementation;

/// <summary>
/// Enumerates the priorities of a <see cref="CacheItem"/>.
/// </summary>
[RunTimeOrCompileTime]
public enum CacheItemPriority
{
    /// <summary>
    /// Default priority means "Default" for <c>System.Runtime.Caching.MemoryCache</c> and it means "Normal" for <c>Microsoft.Extensions.Caching.Memory.IMemoryCache</c>. 
    /// </summary>
    Default,

    /// <summary>
    /// Never removed, unless explicitly required through invalidation methods.
    /// </summary>
    NotRemovable,

    /// <summary>
    /// This cache item is removed earlier if the cache needs to be compacted. For <see cref="MemoryCachingBackend"/>, this is the same as <see cref="Default"/>.
    /// </summary>
    Low,

    /// <summary>
    /// This cache item is removed later if the cache needs to be compacted. For <see cref="MemoryCachingBackend"/>, this is the same as <see cref="Default"/>.
    /// </summary>
    High
}