// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Implementation;

namespace Metalama.Patterns.Caching.Backends;

/// <summary>
/// A <see cref="CachingBackend"/> that throws an exception when it's used. This is the active default backend.
/// </summary>
internal sealed class UninitializedCachingBackend : CachingBackend
{
    private static void Throw() => throw new CachingException( "The caching service has not been initialized." );

    /// <param name="options"></param>
    /// <inheritdoc />
    protected override void ClearCore( ClearCacheOptions options ) => Throw();

    /// <inheritdoc />
    protected override bool ContainsDependencyCore( string key )
    {
        Throw();

        return false;
    }

    /// <inheritdoc />
    protected override bool ContainsItemCore( string key )
    {
        Throw();

        return false;
    }

    /// <inheritdoc />
    protected override CacheItem? GetItemCore( string key, bool includeDependencies )
    {
        Throw();

        return null;
    }

    /// <inheritdoc />
    protected override void InvalidateDependencyCore( string key ) => Throw();

    /// <inheritdoc />
    protected override void RemoveItemCore( string key ) => Throw();

    /// <inheritdoc />
    protected override void SetItemCore( string key, CacheItem item ) => Throw();
}