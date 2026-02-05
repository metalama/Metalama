// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using System.Collections.Concurrent;

namespace Metalama.Patterns.Caching.Tests.Mocks;

/// <summary>
/// A thread-safe test caching backend that tracks operations.
/// </summary>
internal sealed class TrackingCachingBackend : CachingBackend
{
    private readonly ConcurrentDictionary<string, CacheItem> _items = new();

    private volatile bool _setItemCalled;
    private volatile string? _lastSetKey;
    private int _setItemCount;
    private volatile bool _removeItemCalled;
    private volatile string? _lastRemovedKey;
    private volatile bool _invalidateDependencyCalled;
    private volatile string? _lastInvalidatedDependency;
    private volatile bool _clearCalled;
    private volatile bool _getItemCalled;

    public bool SetItemCalled => this._setItemCalled;

    public string? LastSetKey => this._lastSetKey;

    public int SetItemCount => Volatile.Read( ref this._setItemCount );

    public bool RemoveItemCalled => this._removeItemCalled;

    public string? LastRemovedKey => this._lastRemovedKey;

    public bool InvalidateDependencyCalled => this._invalidateDependencyCalled;

    public string? LastInvalidatedDependency => this._lastInvalidatedDependency;

    public bool ClearCalled => this._clearCalled;

    public bool GetItemCalled => this._getItemCalled;

    public TrackingCachingBackend( string debugName, IServiceProvider serviceProvider )
        : base( new MemoryCachingBackendConfiguration { DebugName = debugName }, serviceProvider ) { }

    public void ResetTracking()
    {
        this._setItemCalled = false;
        this._lastSetKey = null;
        Interlocked.Exchange( ref this._setItemCount, 0 );
        this._removeItemCalled = false;
        this._lastRemovedKey = null;
        this._invalidateDependencyCalled = false;
        this._lastInvalidatedDependency = null;
        this._clearCalled = false;
        this._getItemCalled = false;
    }

    protected override void SetItemCore( string key, CacheItem item )
    {
        this._setItemCalled = true;
        this._lastSetKey = key;
        Interlocked.Increment( ref this._setItemCount );
        this._items[key] = item;
    }

    protected override ValueTask SetItemAsyncCore( string key, CacheItem item, CancellationToken cancellationToken )
    {
        this.SetItemCore( key, item );

        return default;
    }

    protected override CacheItem? GetItemCore( string key, bool includeDependencies )
    {
        this._getItemCalled = true;

        return this._items.TryGetValue( key, out var item ) ? item : null;
    }

    protected override ValueTask<CacheItem?> GetItemAsyncCore( string key, bool includeDependencies, CancellationToken cancellationToken )
    {
        return new ValueTask<CacheItem?>( this.GetItemCore( key, includeDependencies ) );
    }

    protected override bool ContainsItemCore( string key )
    {
        return this._items.ContainsKey( key );
    }

    protected override ValueTask<bool> ContainsItemAsyncCore( string key, CancellationToken cancellationToken )
    {
        return new ValueTask<bool>( this.ContainsItemCore( key ) );
    }

    protected override void RemoveItemCore( string key )
    {
        this._removeItemCalled = true;
        this._lastRemovedKey = key;
        this._items.TryRemove( key, out _ );
    }

    protected override ValueTask RemoveItemAsyncCore( string key, CancellationToken cancellationToken )
    {
        this.RemoveItemCore( key );

        return default;
    }

    protected override void InvalidateDependencyCore( string key )
    {
        this._invalidateDependencyCalled = true;
        this._lastInvalidatedDependency = key;

        // Remove items with this dependency
        var keysToRemove = this._items
            .Where( kv => kv.Value.Dependencies.Contains( key ) )
            .Select( kv => kv.Key )
            .ToList();

        foreach ( var k in keysToRemove )
        {
            this._items.TryRemove( k, out _ );
        }
    }

    protected override ValueTask InvalidateDependencyAsyncCore( string key, CancellationToken cancellationToken )
    {
        this.InvalidateDependencyCore( key );

        return default;
    }

    protected override bool ContainsDependencyCore( string key )
    {
        return this._items.Values.Any( item => item.Dependencies.Contains( key ) );
    }

    protected override void ClearCore( ClearCacheOptions options )
    {
        this._clearCalled = true;
        this._items.Clear();
    }

    protected override ValueTask ClearAsyncCore( ClearCacheOptions options, CancellationToken cancellationToken )
    {
        this.ClearCore( options );

        return default;
    }

    protected override CachingBackendFeatures CreateFeatures()
    {
        return new TrackingBackendFeatures();
    }

    private sealed class TrackingBackendFeatures : CachingBackendFeatures
    {
        public override bool Blocking => true;
    }
}
