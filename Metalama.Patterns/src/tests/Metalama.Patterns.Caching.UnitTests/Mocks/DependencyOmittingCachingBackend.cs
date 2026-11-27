// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace Metalama.Patterns.Caching.Tests.Mocks;

/// <summary>
/// A test caching backend that returns the dependencies of a cache item only when the caller requests them, and that
/// records every cache lookup.
/// </summary>
/// <remarks>
/// <see cref="MemoryCachingBackend"/> returns the dependencies of a cache item even when the caller does not request them.
/// A test that runs on it therefore cannot detect a caller that does not request the dependencies it needs. This backend
/// behaves like a backend that needs an additional request to retrieve the dependencies.
/// </remarks>
internal sealed class DependencyOmittingCachingBackend : CachingBackendEnhancer
{
    private readonly ConcurrentQueue<GetItemRequest> _getItemRequests = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DependencyOmittingCachingBackend"/> class.
    /// </summary>
    /// <param name="underlyingBackend">The backend that stores the cache items.</param>
    public DependencyOmittingCachingBackend( CachingBackend underlyingBackend ) : base( underlyingBackend ) { }

    /// <summary>
    /// Gets the cache lookups since the last call of <see cref="ClearGetItemRequests"/>, in the order in which they were
    /// made.
    /// </summary>
    public IReadOnlyList<GetItemRequest> GetItemRequests => this._getItemRequests.ToArray();

    /// <summary>
    /// Gets the key of the last item that was stored.
    /// </summary>
    public string? LastSetKey { get; private set; }

    /// <summary>
    /// Removes the recorded cache lookups.
    /// </summary>
    public void ClearGetItemRequests()
    {
        while ( this._getItemRequests.TryDequeue( out _ ) ) { }
    }

    /// <inheritdoc />
    protected override void SetItemCore( string key, CacheItem item )
    {
        this.LastSetKey = key;
        base.SetItemCore( key, item );
    }

    /// <inheritdoc />
    protected override ValueTask SetItemAsyncCore( string key, CacheItem item, CancellationToken cancellationToken )
    {
        this.LastSetKey = key;

        return base.SetItemAsyncCore( key, item, cancellationToken );
    }

    /// <inheritdoc />
    protected override bool SetItemCore( string key, CacheItem item, DateTimeOffset ifUnmodifiedSince )
    {
        this.LastSetKey = key;

        return base.SetItemCore( key, item, ifUnmodifiedSince );
    }

    /// <inheritdoc />
    protected override ValueTask<bool> SetItemAsyncCore(
        string key,
        CacheItem item,
        DateTimeOffset ifUnmodifiedSince,
        CancellationToken cancellationToken )
    {
        this.LastSetKey = key;

        return base.SetItemAsyncCore( key, item, ifUnmodifiedSince, cancellationToken );
    }

    /// <inheritdoc />
    protected override CacheItem? GetItemCore( string key, bool includeDependencies )
    {
        this._getItemRequests.Enqueue( new GetItemRequest( key, includeDependencies ) );

        return OmitDependenciesUnlessRequested( base.GetItemCore( key, includeDependencies ), includeDependencies );
    }

    /// <inheritdoc />
    protected override async ValueTask<CacheItem?> GetItemAsyncCore( string key, bool includeDependencies, CancellationToken cancellationToken )
    {
        this._getItemRequests.Enqueue( new GetItemRequest( key, includeDependencies ) );

        return OmitDependenciesUnlessRequested( await base.GetItemAsyncCore( key, includeDependencies, cancellationToken ), includeDependencies );
    }

    /// <summary>
    /// Returns a copy of a cache item without its dependencies when the caller did not request them.
    /// </summary>
    /// <param name="item">The cache item returned by the underlying backend.</param>
    /// <param name="includeDependencies">Whether the caller requested the dependencies.</param>
    /// <returns><paramref name="item"/>, or a copy of it with no dependencies.</returns>
    private static CacheItem? OmitDependenciesUnlessRequested( CacheItem? item, bool includeDependencies )
        => item == null || includeDependencies ? item : item with { Dependencies = ImmutableArray<string>.Empty };

    /// <summary>
    /// A cache lookup recorded by <see cref="DependencyOmittingCachingBackend"/>.
    /// </summary>
    /// <param name="Key">The key of the requested item.</param>
    /// <param name="IncludeDependencies">Whether the caller requested the dependencies of the item.</param>
    public sealed record GetItemRequest( string Key, bool IncludeDependencies );
}
