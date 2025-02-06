// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

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