// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Implementation;
using System.Globalization;

namespace Metalama.Patterns.Caching.Backends;

/// <summary>
/// A <see cref="CachingBackendEnhancer"/> that adds a local (fast) <see cref="MemoryCachingBackend"/> to a remote (slower) cache.
/// This class is typically instantiated in the back-end factory method. You should normally not use this class unless you develop a custom caching back-end.
/// </summary>
[PublicAPI]
internal sealed class LayeredCachingBackendEnhancer : CachingBackendEnhancer
{
    private readonly TimeSpan _removedItemTransitionPeriod = TimeSpan.FromMinutes( 1 );

    /// <summary>
    /// Gets the in-memory local cache.
    /// </summary>
    public MemoryCachingBackend LocalCache { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="LayeredCachingBackendEnhancer"/> class.
    /// Initializes a new <see cref="LayeredCachingBackendEnhancer"/>.
    /// </summary>
    /// <param name="remoteCache">The remote <see cref="CachingBackend"/>.</param>
    /// <param name="memoryCache">A <see cref="MemoryCachingBackend"/>, or <c>null</c> to use a new default <see cref="MemoryCachingBackend"/>.</param>
    /// <param name="l1CachingBackendConfiguration"></param>
    public LayeredCachingBackendEnhancer(
        CachingBackend remoteCache,
        MemoryCachingBackend? memoryCache,
        LayeredCachingBackendConfiguration? configuration ) : base(
        remoteCache,
        configuration )
    {
        this.LocalCache = memoryCache ?? new MemoryCachingBackend(
            serviceProvider: remoteCache.ServiceProvider,
            configuration: configuration?.L1Configuration );
    }

    protected override async Task InitializeCoreAsync( CancellationToken cancellationToken = default )
    {
        await base.InitializeCoreAsync( cancellationToken );
        await this.LocalCache.InitializeAsync( cancellationToken );
    }

    protected override void InitializeCore()
    {
        base.InitializeCore();
        this.LocalCache.Initialize();
    }

    /// <inheritdoc />
    protected override void OnBackendDependencyInvalidated( object? sender, CacheDependencyInvalidatedEventArgs args )
    {
        if ( args.SourceId != this.UnderlyingBackend.Id )
        {
            this.LocalCache.InvalidateDependency( args.Key );
        }

        base.OnBackendDependencyInvalidated( sender, args );
    }

    /// <inheritdoc />
    protected override void OnBackendItemRemoved( object? sender, CacheItemRemovedEventArgs args )
    {
        if ( args.SourceId != this.UnderlyingBackend.Id )
        {
            this.LocalCache.RemoveItem( args.Key );
        }

        base.OnBackendItemRemoved( sender, args );
    }

    /// <inheritdoc />
    protected override void SetItemCore( string key, CacheItem item )
    {
        var newItem = new MaterializedCacheItem( item );

        this.LocalCache.SetItem( key, item );
        this.UnderlyingBackend.SetItem( key, newItem );
    }

    /// <inheritdoc />
    protected override ValueTask SetItemAsyncCore( string key, CacheItem item, CancellationToken cancellationToken )
    {
        var newItem = new MaterializedCacheItem( item );

        this.LocalCache.SetItem( key, item );

        return this.UnderlyingBackend.SetItemAsync( key, newItem, cancellationToken );
    }

    /// <inheritdoc />
    protected override bool ContainsItemCore( string key )
    {
        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            return this.LocalCache.ContainsItem( key ) || this.UnderlyingBackend.ContainsItem( key );
        }
        else
        {
            return this.GetItemCore( key, false ) != null;
        }
    }

    /// <inheritdoc />
    protected override async ValueTask<bool> ContainsItemAsyncCore( string key, CancellationToken cancellationToken )
    {
        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
            return this.LocalCache.ContainsItem( key ) || await this.UnderlyingBackend.ContainsItemAsync( key, cancellationToken );
        }
        else
        {
            return await this.GetItemAsyncCore( key, false, cancellationToken ) != null;
        }
    }

    /// <inheritdoc />
    protected override bool ContainsDependencyCore( string key )
    {
        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            return this.LocalCache.ContainsDependency( key ) || this.UnderlyingBackend.ContainsDependency( key );
        }
        else
        {
            throw new NotSupportedException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "ContainsDependency is not supported with in {0} with a non-blocking remote backend.",
                    nameof(LayeredCachingBackendEnhancer) ) );
        }
    }

    /// <inheritdoc />
    protected override async ValueTask<bool> ContainsDependencyAsyncCore( string key, CancellationToken cancellationToken )
    {
        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            // ReSharper disable once MethodHasAsyncOverloadWithCancellation
            return this.LocalCache.ContainsDependency( key ) || await this.UnderlyingBackend.ContainsDependencyAsync( key, cancellationToken );
        }
        else
        {
            throw new NotSupportedException(
                string.Format(
                    CultureInfo.InvariantCulture,
                    "ContainsDependencyAsync is not supported with in {0} with a non-blocking remote backend.",
                    nameof(LayeredCachingBackendEnhancer) ) );
        }
    }

    /// <inheritdoc />
    protected override CacheItem? GetItemCore( string key, bool includeDependencies )
    {
        var localCacheItem = this.LocalCache.GetItem( key, includeDependencies );

        if ( localCacheItem == null )
        {
            var remoteCacheItem = this.GetValueFromUnderlyingBackend( key );

            if ( remoteCacheItem != null )
            {
                // We have a value stored remotely.
                // Cache in local memory.
                this.SetMemoryCacheFromRemote( key, remoteCacheItem );

                return remoteCacheItem;
            }
            else
            {
                return null;
            }
        }
        else
        {
            switch ( localCacheItem )
            {
                case RemovedValue removedValue:
                    {
                        // We have the magic string meaning that the node has been deleted.

                        var remoteCacheItem = this.GetValueFromUnderlyingBackend( key );

                        if ( remoteCacheItem == null )
                        {
                            return null;
                        }
                        else
                        {
                            if ( remoteCacheItem.Timestamp > removedValue.Timestamp )
                            {
                                this.SetMemoryCacheFromRemote( key, remoteCacheItem );

                                return remoteCacheItem;
                            }
                            else
                            {
                                // The remote value is older than the deletion.
                                return null;
                            }
                        }
                    }

                default:
                    return localCacheItem;
            }
        }
    }

    private MaterializedCacheItem? GetValueFromUnderlyingBackend( string key )
    {
        var cacheValue = (MaterializedCacheItem?) this.UnderlyingBackend.GetItem( key );

        if ( cacheValue == null )
        {
            return null;
        }

        return cacheValue;
    }

    private async Task<MaterializedCacheItem?> GetValueFromUnderlyingBackendAsync( string key, CancellationToken cancellationToken )
    {
        var cacheValue = (MaterializedCacheItem?) await this.UnderlyingBackend.GetItemAsync( key, true, cancellationToken );

        if ( cacheValue == null )
        {
            return null;
        }

        return cacheValue;
    }

    private void SetMemoryCacheFromRemote( string key, MaterializedCacheItem remoteCacheValue )
    {
        this.LocalCache.SetItem( key, remoteCacheValue );
    }

    /// <inheritdoc />
    protected override async ValueTask<CacheItem?> GetItemAsyncCore( string key, bool includeDependencies, CancellationToken cancellationToken )
    {
        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
        var localCacheValue = this.LocalCache.GetItem( key, includeDependencies );

        if ( localCacheValue == null )
        {
            var remoteCacheItem = await this.GetValueFromUnderlyingBackendAsync( key, cancellationToken );

            if ( remoteCacheItem != null )
            {
                // We have a value stored remotely.
                // Cache in local memory.
                this.SetMemoryCacheFromRemote( key, remoteCacheItem );

                return remoteCacheItem;
            }
            else
            {
                return null;
            }
        }
        else
        {
            switch ( localCacheValue )
            {
                case RemovedValue removedValue:
                    {
                        // We have the magic string meaning that the node has been deleted.

                        var remoteCacheItem = await this.GetValueFromUnderlyingBackendAsync( key, cancellationToken );

                        if ( remoteCacheItem == null )
                        {
                            return null;
                        }
                        else
                        {
                            var multiLayerCacheValue = (MaterializedCacheItem) (remoteCacheItem.Value
                                                                                ?? throw new CachingAssertionFailedException( "null not expected." ));

                            if ( multiLayerCacheValue.Timestamp > removedValue.Timestamp )
                            {
                                this.SetMemoryCacheFromRemote( key, remoteCacheItem );

                                return remoteCacheItem;
                            }
                            else
                            {
                                // The remote value is older than the deletion.
                                return null;
                            }
                        }
                    }

                default:
                    return localCacheValue;
            }
        }
    }

    /// <inheritdoc />
    protected override void InvalidateDependencyCore( string key )
    {
        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            this.LocalCache.InvalidateDependency( key );
        }
        else
        {
            this.LocalCache.InvalidateDependencyImpl( key, new RemovedValue(), DateTimeOffset.UtcNow + this._removedItemTransitionPeriod );
        }

        this.UnderlyingBackend.InvalidateDependency( key );
    }

    /// <inheritdoc />
    protected override ValueTask InvalidateDependencyAsyncCore( string key, CancellationToken cancellationToken )
    {
        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            this.LocalCache.InvalidateDependency( key );
        }
        else
        {
            this.LocalCache.InvalidateDependencyImpl( key, new RemovedValue(), DateTimeOffset.UtcNow + this._removedItemTransitionPeriod );
        }

        return this.UnderlyingBackend.InvalidateDependencyAsync( key, cancellationToken );
    }

    /// <inheritdoc />
    protected override void RemoveItemCore( string key )
    {
        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            this.LocalCache.RemoveItem( key );
        }
        else
        {
            this.LocalCache.RemoveItemImpl( key, new RemovedValue(), DateTimeOffset.UtcNow + this._removedItemTransitionPeriod );
        }

        this.UnderlyingBackend.RemoveItem( key );
    }

    /// <inheritdoc />
    protected override ValueTask RemoveItemAsyncCore( string key, CancellationToken cancellationToken )
    {
        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            this.LocalCache.RemoveItem( key );
        }
        else
        {
            this.LocalCache.RemoveItemImpl( key, new RemovedValue(), DateTime.UtcNow + this._removedItemTransitionPeriod );
        }

        return this.UnderlyingBackend.RemoveItemAsync( key, cancellationToken );
    }

    /// <param name="options"></param>
    /// <inheritdoc />
    protected override void ClearCore( ClearCacheOptions options )
    {
        this.LocalCache.Clear();

        if ( options == ClearCacheOptions.Default )
        {
            this.UnderlyingBackend.Clear();
        }
    }

    /// <inheritdoc />
    protected override ValueTask ClearAsyncCore( ClearCacheOptions options, CancellationToken cancellationToken )
    {
        this.LocalCache.Clear();

        if ( options == ClearCacheOptions.Default )
        {
            return this.UnderlyingBackend.ClearAsync( options, cancellationToken );
        }
        else
        {
            return default;
        }
    }

    /// <inheritdoc />
    protected override void DisposeCore( bool disposing, CancellationToken cancellationToken )
    {
        base.DisposeCore( disposing, cancellationToken );

        this.LocalCache.Dispose( cancellationToken );
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore( CancellationToken cancellationToken )
    {
        await base.DisposeAsyncCore( cancellationToken );
        await this.LocalCache.DisposeAsync( cancellationToken );
    }

    /// <inheritdoc />
    protected override CachingBackendFeatures CreateFeatures() => new Features( this.UnderlyingBackend.SupportedFeatures );

    internal static long GetTimestamp() => DateTimeOffset.UtcNow.UtcTicks;

    private sealed class Features : CachingBackendEnhancerFeatures
    {
        public Features( CachingBackendFeatures underlyingBackendFeatures ) : base( underlyingBackendFeatures ) { }

        public override bool ContainsDependency => this.UnderlyingBackendFeatures is { ContainsDependency: true, Blocking: true };
    }

    private sealed record RemovedValue : MemoryCacheItem
    {
#pragma warning disable SA1401
        public readonly long Timestamp = GetTimestamp();
#pragma warning restore SA1401

        public RemovedValue() : base( null, default, new object() ) { }
    }
}