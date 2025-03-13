// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Backends;

/// <summary>
/// An implementation of <see cref="CachingBackend"/> that does not cache at all.
/// </summary>
[PublicAPI]
internal class NullCachingBackend : CachingBackend
{
    /// <inheritdoc />
    protected override void SetItemCore( string key, CacheItem item ) { }

    /// <inheritdoc />
    protected override bool ContainsItemCore( string key ) => false;

    /// <inheritdoc />
    protected override CacheItem? GetItemCore( string key, bool includeDependencies ) => null;

    /// <inheritdoc />
    protected override void InvalidateDependencyCore( string key ) { }

    /// <inheritdoc />
    protected override bool ContainsDependencyCore( string key ) => false;

    /// <param name="options"></param>
    /// <inheritdoc />
    protected override void ClearCore( ClearCacheOptions options ) { }

    /// <inheritdoc />
    protected override void RemoveItemCore( string key ) { }
}