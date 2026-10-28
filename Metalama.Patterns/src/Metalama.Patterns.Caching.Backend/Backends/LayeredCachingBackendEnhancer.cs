// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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
    /// A counter incremented before every removal, invalidation or clearing that can affect the local cache. A read that
    /// copies a value of the remote cache into the local cache compares it before and after the copy, so that a value read
    /// before a concurrent removal does not stay in the local cache.
    /// </summary>
    private long _removalVersion;

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
        this.IncrementRemovalVersion();

        if ( args.SourceId != this.UnderlyingBackend.Id )
        {
            this.LocalCache.InvalidateDependency( args.Key );
        }

        base.OnBackendDependencyInvalidated( sender, args );
    }

    /// <inheritdoc />
    protected override void OnBackendItemRemoved( object? sender, CacheItemRemovedEventArgs args )
    {
        this.IncrementRemovalVersion();

        if ( args.SourceId != this.UnderlyingBackend.Id )
        {
            this.LocalCache.RemoveItem( args.Key );
        }

        base.OnBackendItemRemoved( sender, args );
    }

    /// <inheritdoc />
    protected override void SetItemCore( string key, CacheItem item )
    {
        var newItem = new MaterializedCacheItem( item, this.TimeProvider );

        this.LocalCache.SetItem( key, item );
        this.UnderlyingBackend.SetItem( key, newItem );
    }

    /// <inheritdoc />
    protected override ValueTask SetItemAsyncCore( string key, CacheItem item, CancellationToken cancellationToken )
    {
        var newItem = new MaterializedCacheItem( item, this.TimeProvider );

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

        if ( localCacheItem == null || (includeDependencies && localCacheItem.Dependencies.IsDefault) )
        {
            // The dependencies are always requested, because the item is stored in the local cache, which must register
            // them so that a local invalidation removes it.
            var removalVersion = this.GetRemovalVersion();
            var remoteCacheItem = this.GetValueFromUnderlyingBackend( key, true );

            if ( remoteCacheItem != null )
            {
                // We have a value stored remotely.
                // Cache in local memory.
                this.SetMemoryCacheFromRemote( key, remoteCacheItem, removalVersion );

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

                        var removalVersion = this.GetRemovalVersion();

                        var remoteCacheItem = this.GetValueFromUnderlyingBackend( key, true );

                        if ( remoteCacheItem == null )
                        {
                            return null;
                        }
                        else
                        {
                            if ( remoteCacheItem.Timestamp > removedValue.Timestamp )
                            {
                                this.SetMemoryCacheFromRemote( key, remoteCacheItem, removalVersion );

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

    private MaterializedCacheItem? GetValueFromUnderlyingBackend( string key, bool includeDependencies )
    {
        var cacheValue = (MaterializedCacheItem?) this.UnderlyingBackend.GetItem( key, includeDependencies );

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

    /// <summary>
    /// Copies a value read from the remote cache into the local cache, unless a removal, an invalidation or a clearing
    /// started after <paramref name="removalVersion"/> was read.
    /// </summary>
    /// <remarks>
    /// The version is compared after the copy. A removal increments the version before it removes the local item. Either
    /// the removal runs after the copy and removes it, or the comparison observes the increment and the copy is removed
    /// here. A removal of an unrelated key also withdraws the copy, which only costs a later read of the remote cache.
    /// </remarks>
    private void SetMemoryCacheFromRemote( string key, MaterializedCacheItem remoteCacheValue, long removalVersion )
    {
        this.LocalCache.SetItem( key, remoteCacheValue );

        if ( this.GetRemovalVersion() != removalVersion )
        {
            this.LocalCache.RemoveItemImpl( key );
        }
    }

    private long GetRemovalVersion() => Interlocked.Read( ref this._removalVersion );

    private void IncrementRemovalVersion() => Interlocked.Increment( ref this._removalVersion );

    /// <inheritdoc />
    protected override async ValueTask<CacheItem?> GetItemAsyncCore( string key, bool includeDependencies, CancellationToken cancellationToken )
    {
        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
        var localCacheValue = this.LocalCache.GetItem( key, includeDependencies );

        if ( localCacheValue == null )
        {
            var removalVersion = this.GetRemovalVersion();
            var remoteCacheItem = await this.GetValueFromUnderlyingBackendAsync( key, cancellationToken );

            if ( remoteCacheItem != null )
            {
                // We have a value stored remotely.
                // Cache in local memory.
                this.SetMemoryCacheFromRemote( key, remoteCacheItem, removalVersion );

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

                        var removalVersion = this.GetRemovalVersion();

                        var remoteCacheItem = await this.GetValueFromUnderlyingBackendAsync( key, cancellationToken );

                        if ( remoteCacheItem == null )
                        {
                            return null;
                        }
                        else
                        {
                            if ( remoteCacheItem.Timestamp > removedValue.Timestamp )
                            {
                                this.SetMemoryCacheFromRemote( key, remoteCacheItem, removalVersion );

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
        this.IncrementRemovalVersion();

        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            this.LocalCache.InvalidateDependency( key );
        }
        else
        {
            this.LocalCache.InvalidateDependencyImpl( key, new RemovedValue( this.GetTimestamp() ), this.TimeProvider.GetUtcNow() + this._removedItemTransitionPeriod );
        }

        this.UnderlyingBackend.InvalidateDependency( key );
    }

    /// <inheritdoc />
    protected override ValueTask InvalidateDependencyAsyncCore( string key, CancellationToken cancellationToken )
    {
        this.IncrementRemovalVersion();

        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            this.LocalCache.InvalidateDependency( key );
        }
        else
        {
            this.LocalCache.InvalidateDependencyImpl( key, new RemovedValue( this.GetTimestamp() ), this.TimeProvider.GetUtcNow() + this._removedItemTransitionPeriod );
        }

        return this.UnderlyingBackend.InvalidateDependencyAsync( key, cancellationToken );
    }

    /// <inheritdoc />
    protected override void RemoveItemCore( string key )
    {
        this.IncrementRemovalVersion();

        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            this.LocalCache.RemoveItem( key );
        }
        else
        {
            this.LocalCache.RemoveItemImpl( key, new RemovedValue( this.GetTimestamp() ), this.TimeProvider.GetUtcNow() + this._removedItemTransitionPeriod );
        }

        this.UnderlyingBackend.RemoveItem( key );
    }

    /// <inheritdoc />
    protected override ValueTask RemoveItemAsyncCore( string key, CancellationToken cancellationToken )
    {
        this.IncrementRemovalVersion();

        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            this.LocalCache.RemoveItem( key );
        }
        else
        {
            this.LocalCache.RemoveItemImpl( key, new RemovedValue( this.GetTimestamp() ), this.TimeProvider.GetUtcNow() + this._removedItemTransitionPeriod );
        }

        return this.UnderlyingBackend.RemoveItemAsync( key, cancellationToken );
    }

    /// <param name="options"></param>
    /// <inheritdoc />
    protected override void ClearCore( ClearCacheOptions options )
    {
        this.IncrementRemovalVersion();

        this.LocalCache.Clear();

        if ( options == ClearCacheOptions.Default )
        {
            this.UnderlyingBackend.Clear();
        }
    }

    /// <inheritdoc />
    protected override ValueTask ClearAsyncCore( ClearCacheOptions options, CancellationToken cancellationToken )
    {
        this.IncrementRemovalVersion();

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

    /// <summary>
    /// Gets the current instant, in ticks, from the clock of the backend. The value is the timestamp of a
    /// materialized cache item and of a removal.
    /// </summary>
    internal long GetTimestamp() => this.TimeProvider.GetUtcNow().UtcTicks;

    private sealed class Features : CachingBackendEnhancerFeatures
    {
        public Features( CachingBackendFeatures underlyingBackendFeatures ) : base( underlyingBackendFeatures ) { }

        public override bool ContainsDependency => this.UnderlyingBackendFeatures is { ContainsDependency: true, Blocking: true };
    }

    private sealed record RemovedValue : MemoryCacheItem
    {
#pragma warning disable SA1401
        public readonly long Timestamp;
#pragma warning restore SA1401

        /// <summary>
        /// Initializes a new instance of the <see cref="RemovedValue"/> class.
        /// </summary>
        /// <param name="timestamp">
        /// The instant of the removal, read from the clock of the enhancer by <see cref="GetTimestamp"/>.
        /// </param>
        public RemovedValue( long timestamp ) : base( null, default, new object() )
        {
            this.Timestamp = timestamp;
        }
    }
}