// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Implementation;
using SharpCrafters.Common.Testing.Hooks;
using System.Collections.Concurrent;
using System.Globalization;
using static Flashtrace.Messages.FormattedMessageBuilder;

namespace Metalama.Patterns.Caching.Backends;

/// <summary>
/// A <see cref="CachingBackendEnhancer"/> that adds a local (fast) <see cref="MemoryCachingBackend"/> to a remote (slower) cache.
/// This class is typically instantiated in the back-end factory method. You should normally not use this class unless you develop a custom caching back-end.
/// </summary>
/// <remarks>
/// <para>
/// A read that finds no item in the local cache reads the remote cache and copies the item into the local cache. Such a
/// read is registered in <see cref="_pendingReadThroughs"/> for its duration, so that a removal, an invalidation or a
/// clearing that overlaps the read can mark it. A marked read withdraws its copy from the local cache.
/// </para>
/// <para>
/// When the remote cache is blocking, a removal changes the local cache, then the remote cache. It then marks the
/// pending reads and changes the local cache again. A read that overlaps the change of the remote cache is therefore
/// either marked, or has copied its item before the second change of the local cache, which removes it.
/// </para>
/// <para>
/// When the remote cache is not blocking, the change of the remote cache is still pending when the removal returns. The
/// removal therefore replaces the local item with a replacement value (tombstone) that carries the timestamp of the
/// removal. A read does not copy a remote item that is not newer than the replacement value.
/// </para>
/// <para>
/// A read that finds a replacement value returns the remote item only if <see cref="MaterializedCacheItem.Timestamp"/> is
/// strictly greater than <see cref="RemovedValue.Timestamp"/>. Both timestamps come from <see cref="GetTimestamp"/>, which
/// reads the clock of the backend, the system clock by default, and the remote item may have been written by another
/// machine. The remarks of <see cref="CachingBackendFeatures.Blocking"/> document the resulting assumption and its failure
/// modes.
/// </para>
/// <para>
/// A read requests the dependencies of an item from the remote cache only when its caller requests them, because the
/// remote cache may need additional round trips to read them. The local cache therefore cannot register the
/// dependencies of an item that it has copied without them. When a dependency is invalidated through this backend, the
/// local cache removes every such copy, because any of them can depend on the invalidated dependency, and it removes
/// the items that depend on the key of such a copy for the same reason. When the remote cache is not blocking, it
/// replaces them with replacement values, so an item that does not depend on the invalidated dependency is reported as
/// absent until it is stored again or until the replacement value expires. The other nodes do not do so: they rely on
/// the <see cref="CachingBackend.ItemRemoved"/> event that the remote cache must raise for each item that it removes
/// during an invalidation.
/// </para>
/// <para>
/// The <see cref="CachingBackend.ItemRemoved"/> event of the remote cache removes the item from the local cache, also
/// when the event results from an operation of this node. The local cache cannot always remove the item itself: it
/// does not know the dependencies of every copy, and it keeps the items that depend on a key whose item it does not
/// hold when a new value is stored for that key, whereas the remote cache removes them if it holds a value for the key.
/// </para>
/// </remarks>
[PublicAPI]
internal sealed partial class LayeredCachingBackendEnhancer : CachingBackendEnhancer
{
    private readonly TimeSpan _removedItemTransitionPeriod = TimeSpan.FromMinutes( 1 );

    /// <summary>
    /// The optional test synchronization service, resolved once from the service provider of the remote backend. It is
    /// never registered in production, in which case it stays <see langword="null"/> and every synchronization point is
    /// skipped.
    /// </summary>
    private readonly ITestSynchronizationProvider? _testSynchronizationProvider;

    /// <summary>
    /// The reads that are reading an item from the remote cache or copying it into the local cache.
    /// </summary>
    private readonly ConcurrentDictionary<ReadThrough, bool> _pendingReadThroughs = new();

    /// <summary>
    /// The number of reads that are registered, or are being registered, in <see cref="_pendingReadThroughs"/>. It lets a
    /// removal skip the enumeration of <see cref="_pendingReadThroughs"/> when no read is pending.
    /// </summary>
    private int _pendingReadThroughCount;

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

        this._testSynchronizationProvider =
            (ITestSynchronizationProvider?) remoteCache.ServiceProvider.GetService( typeof(ITestSynchronizationProvider) );
    }

    protected override async Task InitializeCoreAsync( CancellationToken cancellationToken = default )
    {
        await base.InitializeCoreAsync( cancellationToken );
        await this.LocalCache.InitializeAsync( cancellationToken );
        this.WarnIfUnderlyingBackendIsNonBlocking();
    }

    protected override void InitializeCore()
    {
        base.InitializeCore();
        this.LocalCache.Initialize();
        this.WarnIfUnderlyingBackendIsNonBlocking();
    }

    private void WarnIfUnderlyingBackendIsNonBlocking()
    {
        if ( !this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            this.LogSource.Warning.Write(
                Formatted(
                    "The underlying backend '{Backend}' is non-blocking. When an item is removed or invalidated, the L1 cache "
                    + "records the removal for {TransitionPeriod} with a timestamp taken from the clock of this machine, and a value "
                    + "read from the underlying backend during this period is returned only if its own timestamp, taken from the clock "
                    + "of the machine that wrote it, is strictly greater. This requires the clocks of all machines sharing the backend "
                    + "to be synchronized. If the clock of the writing machine is behind the clock of this machine by some offset, a value "
                    + "written within that offset after the removal receives a timestamp that is not greater than the timestamp of the "
                    + "removal, and is reported as absent until the record expires. If the clock of the writing machine is ahead by some "
                    + "offset, a value written within that offset before the removal receives a greater timestamp, and is returned and "
                    + "stored in the L1 cache although the pending removal is about to delete it from the underlying backend.",
                    this.UnderlyingBackend,
                    this._removedItemTransitionPeriod ) );
        }
    }

    /// <inheritdoc />
    protected override void OnBackendDependencyInvalidated( object? sender, CacheDependencyInvalidatedEventArgs args )
    {
        // The remote cache has already changed. The pending reads are marked before the local cache changes.
        this.InvalidateReadThroughs( null );

        if ( args.SourceId != this.UnderlyingBackend.Id )
        {
            this.LocalCache.InvalidateDependency( args.Key );
        }

        base.OnBackendDependencyInvalidated( sender, args );
    }

    /// <inheritdoc />
    protected override void OnBackendItemRemoved( object? sender, CacheItemRemovedEventArgs args )
    {
        // The remote cache has already changed. The pending reads are marked before the local cache changes.
        this.InvalidateReadThroughs( args.Key );

        if ( args.SourceId != this.UnderlyingBackend.Id )
        {
            this.LocalCache.RemoveItem( args.Key );
        }
        else
        {
            // The operation of this node that removed the item from the remote cache has usually updated the local
            // cache already, but not in two cases. First, the local cache does not know the dependencies of a copy that
            // was read without them. Second, the two caches can disagree on whether a new value replaces a value: when
            // an item is absent from the local cache only, storing a new value for its key removes the items that
            // depend on the key from the remote cache and keeps them in the local cache. The item is therefore removed
            // from the local cache too. An item that was stored after the reported removal is removed as well, and is
            // then read again from the remote cache. A replacement value is kept, because it can belong to a removal
            // that is more recent than the reported one.
            this.LocalCache.RemoveLiveItem( args.Key );
        }

        base.OnBackendItemRemoved( sender, args );
    }

    /// <inheritdoc />
    protected override void SetItemCore( string key, CacheItem item )
    {
        var newItem = this.Materialize( item );

        this.LocalCache.SetItem( key, item );
        this.UnderlyingBackend.SetItem( key, newItem );
    }

    /// <inheritdoc />
    protected override ValueTask SetItemAsyncCore( string key, CacheItem item, CancellationToken cancellationToken )
    {
        var newItem = this.Materialize( item );

        this.LocalCache.SetItem( key, item );

        return this.UnderlyingBackend.SetItemAsync( key, newItem, cancellationToken );
    }

    /// <inheritdoc />
    /// <remarks>
    /// The item is stored in the local cache first, as by <see cref="SetItemCore"/>, then in the remote cache under the
    /// condition. When the remote cache rejects the item, the local copy is removed, unless it has already been replaced.
    /// The condition is evaluated by the remote cache only, with its clock, because each invalidation also reaches the
    /// remote cache. An invalidation whose event reaches this node before the local copy is stored has already been
    /// recorded by the remote cache, so that the remote cache rejects the item.
    /// </remarks>
    protected override bool SetItemIfNotInvalidatedCore( string key, CacheItem item, DateTimeOffset computationStartTime )
    {
        var newItem = this.Materialize( item );

        var localItem = this.LocalCache.SetItemImpl( key, item );

        if ( this.UnderlyingBackend.SetItemIfNotInvalidated( key, newItem, computationStartTime ) )
        {
            return true;
        }

        this.RemoveRejectedLocalItem( key, localItem );

        return false;
    }

    /// <inheritdoc />
    protected override async ValueTask<bool> SetItemIfNotInvalidatedAsyncCore(
        string key,
        CacheItem item,
        DateTimeOffset computationStartTime,
        CancellationToken cancellationToken )
    {
        var newItem = this.Materialize( item );

        var localItem = this.LocalCache.SetItemImpl( key, item );

        if ( await this.UnderlyingBackend.SetItemIfNotInvalidatedAsync( key, newItem, computationStartTime, cancellationToken ) )
        {
            return true;
        }

        this.RemoveRejectedLocalItem( key, localItem );

        return false;
    }

    private void RemoveRejectedLocalItem( string key, MemoryCacheItem? localItem )
    {
        if ( localItem != null )
        {
            this.LocalCache.RemoveItemImpl( key, expectedItem: localItem );
        }
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

        if ( localCacheItem is RemovedValue removedValue )
        {
            // The local cache holds the replacement value that means that the item has been removed. This case is
            // tested first, because a replacement value has no dependencies and would otherwise be read as a copy
            // whose dependencies are unknown.
            return this.ReadThroughRemote( key, removedValue, includeDependencies );
        }
        else if ( localCacheItem == null || (includeDependencies && localCacheItem.Dependencies.IsDefault) )
        {
            return this.ReadThroughRemote( key, null, includeDependencies );
        }
        else
        {
            return localCacheItem;
        }
    }

    /// <inheritdoc />
    protected override async ValueTask<CacheItem?> GetItemAsyncCore( string key, bool includeDependencies, CancellationToken cancellationToken )
    {
        // ReSharper disable once MethodHasAsyncOverloadWithCancellation
        var localCacheItem = this.LocalCache.GetItem( key, includeDependencies );

        if ( localCacheItem is RemovedValue removedValue )
        {
            // The local cache holds the replacement value that means that the item has been removed. This case is
            // tested first, because a replacement value has no dependencies and would otherwise be read as a copy
            // whose dependencies are unknown.
            return await this.ReadThroughRemoteAsync( key, removedValue, includeDependencies, cancellationToken );
        }
        else if ( localCacheItem == null || (includeDependencies && localCacheItem.Dependencies.IsDefault) )
        {
            return await this.ReadThroughRemoteAsync( key, null, includeDependencies, cancellationToken );
        }
        else
        {
            return localCacheItem;
        }
    }

    /// <summary>
    /// Reads an item from the remote cache and copies it into the local cache.
    /// </summary>
    /// <param name="key">The key of the item.</param>
    /// <param name="removedValue">The replacement value that the local cache holds for the key, or <see langword="null"/>.</param>
    /// <param name="includeDependencies"><see langword="true"/> when the caller requests the dependencies of the item.</param>
    /// <returns>
    /// The item of the remote cache, or <see langword="null"/> when the remote cache has no item or when its item is not
    /// newer than <paramref name="removedValue"/>.
    /// </returns>
    private MaterializedCacheItem? ReadThroughRemote( string key, RemovedValue? removedValue, bool includeDependencies )
    {
        var readThrough = this.BeginReadThrough( key );

        try
        {
            // The dependencies are requested only when the caller requests them. A copy that is stored without its
            // dependencies is removed by the next invalidation of any dependency.
            var remoteCacheItem = (MaterializedCacheItem?) this.UnderlyingBackend.GetItem( key, includeDependencies );

            return this.CompleteReadThrough( readThrough, remoteCacheItem, removedValue );
        }
        finally
        {
            this.EndReadThrough( readThrough );
        }
    }

    /// <summary>
    /// Asynchronously reads an item from the remote cache and copies it into the local cache.
    /// </summary>
    /// <param name="key">The key of the item.</param>
    /// <param name="removedValue">The replacement value that the local cache holds for the key, or <see langword="null"/>.</param>
    /// <param name="includeDependencies"><see langword="true"/> when the caller requests the dependencies of the item.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns>
    /// The item of the remote cache, or <see langword="null"/> when the remote cache has no item or when its item is not
    /// newer than <paramref name="removedValue"/>.
    /// </returns>
    private async ValueTask<MaterializedCacheItem?> ReadThroughRemoteAsync(
        string key,
        RemovedValue? removedValue,
        bool includeDependencies,
        CancellationToken cancellationToken )
    {
        var readThrough = this.BeginReadThrough( key );

        try
        {
            var remoteCacheItem = (MaterializedCacheItem?) await this.UnderlyingBackend.GetItemAsync( key, includeDependencies, cancellationToken );

            return this.CompleteReadThrough( readThrough, remoteCacheItem, removedValue );
        }
        finally
        {
            this.EndReadThrough( readThrough );
        }
    }

    /// <summary>
    /// Copies an item read from the remote cache into the local cache, unless a replacement value masks it, and withdraws
    /// the copy when a removal, an invalidation or a clearing has marked the read.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The copy is conditional and is decided while the local cache holds the lock of the key. The local cache does not
    /// replace a replacement value whose timestamp is not older than the timestamp of the remote item, because that
    /// replacement value belongs to a removal that is more recent than the item.
    /// </para>
    /// <para>
    /// The mark is read after the copy. A removal marks the read before it changes the local cache. Either the removal
    /// changes the local cache after the copy and removes it, or the read observes the mark and withdraws the copy. The
    /// withdrawal removes only the entry that this read has stored, so it removes neither a replacement value nor a
    /// newer item.
    /// </para>
    /// </remarks>
    /// <param name="readThrough">The registration of the read.</param>
    /// <param name="remoteCacheItem">The item read from the remote cache, or <see langword="null"/>.</param>
    /// <param name="removedValue">The replacement value that the local cache held when the read started, or <see langword="null"/>.</param>
    /// <returns>The item that the read returns.</returns>
    private MaterializedCacheItem? CompleteReadThrough( ReadThrough readThrough, MaterializedCacheItem? remoteCacheItem, RemovedValue? removedValue )
    {
        if ( remoteCacheItem == null )
        {
            return null;
        }

        // The comparison is between the clock of the machine that removed the item and the clock of the machine that
        // wrote the remote value. A tie counts as a removal. See the remarks of CachingBackendFeatures.Blocking.
        if ( removedValue != null && remoteCacheItem.Timestamp <= removedValue.Timestamp )
        {
            // The remote value is not newer than the removal.
            return null;
        }

        this._testSynchronizationProvider?.SyncPoint( SyncPoints.RemoteItemRead );

        var storedItem = this.LocalCache.SetItemImpl(
            readThrough.Key,
            remoteCacheItem,
            replacementValue => replacementValue is RemovedValue currentRemovedValue && remoteCacheItem.Timestamp <= currentRemovedValue.Timestamp,
            isCopy: true );

        if ( storedItem != null && readThrough.IsInvalidated )
        {
            this.LocalCache.RemoveItemImpl( readThrough.Key, expectedItem: storedItem );
        }

        return remoteCacheItem;
    }

    /// <summary>
    /// Registers a read of the remote cache, so that a removal, an invalidation or a clearing can mark it.
    /// </summary>
    private ReadThrough BeginReadThrough( string key )
    {
        var readThrough = new ReadThrough( key );

        // The counter is incremented before the read is registered. A removal that reads the counter before the increment
        // has already changed the remote cache, or marks the pending reads again after it has changed the remote cache.
        Interlocked.Increment( ref this._pendingReadThroughCount );
        this._pendingReadThroughs.TryAdd( readThrough, true );

        return readThrough;
    }

    private void EndReadThrough( ReadThrough readThrough )
    {
        this._pendingReadThroughs.TryRemove( readThrough, out _ );
        Interlocked.Decrement( ref this._pendingReadThroughCount );
    }

    /// <summary>
    /// Marks the pending reads of a key, so that they withdraw the copy that they store in the local cache.
    /// </summary>
    /// <param name="key">
    /// The key of the removed item, or <see langword="null"/> to mark every pending read. An invalidation of a dependency
    /// and a clearing mark every pending read, because the items that they remove are not known in advance: an item can
    /// depend on the invalidated dependency through the keys of other items.
    /// </param>
    private void InvalidateReadThroughs( string? key )
    {
        if ( Volatile.Read( ref this._pendingReadThroughCount ) == 0 )
        {
            return;
        }

        foreach ( var pair in this._pendingReadThroughs )
        {
            if ( key == null || string.Equals( pair.Key.Key, key, StringComparison.Ordinal ) )
            {
                pair.Key.IsInvalidated = true;
            }
        }
    }

    /// <inheritdoc />
    protected override void InvalidateDependencyCore( string key )
    {
        this.InvalidateLocalDependency( key );
        this.UnderlyingBackend.InvalidateDependency( key );
        this.CompleteInvalidation( key );
    }

    /// <inheritdoc />
    protected override async ValueTask InvalidateDependencyAsyncCore( string key, CancellationToken cancellationToken )
    {
        this.InvalidateLocalDependency( key );
        await this.UnderlyingBackend.InvalidateDependencyAsync( key, cancellationToken );
        this.CompleteInvalidation( key );
    }

    /// <summary>
    /// Marks every pending read, then invalidates a dependency in the local cache and removes the copies whose
    /// dependencies are unknown, with the items that depend on them. When the remote cache is not blocking, each removed
    /// item is replaced with a replacement value for the transition period.
    /// </summary>
    private void InvalidateLocalDependency( string key )
    {
        this.InvalidateReadThroughs( null );

        // The copies whose dependencies are unknown are not in the dependency index of the local cache, so they are
        // removed separately.
        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            this.LocalCache.InvalidateDependency( key );
            this.LocalCache.RemoveItemsWithUnknownDependencies();
        }
        else
        {
            var removedValue = new RemovedValue( this.GetTimestamp() );
            var expiration = this.TimeProvider.GetUtcNow() + this._removedItemTransitionPeriod;

            this.LocalCache.InvalidateDependencyImpl( key, removedValue, expiration );
            this.LocalCache.RemoveItemsWithUnknownDependencies( removedValue, expiration );
        }
    }

    /// <summary>
    /// Marks every pending read and invalidates a dependency in the local cache again, after a blocking remote cache has
    /// invalidated it. This removes the items that a read has copied from the remote cache between the first local
    /// invalidation and the remote invalidation.
    /// </summary>
    private void CompleteInvalidation( string key )
    {
        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            this.InvalidateReadThroughs( null );
            this.LocalCache.InvalidateDependency( key );
            this.LocalCache.RemoveItemsWithUnknownDependencies();
        }
    }

    /// <inheritdoc />
    protected override void RemoveItemCore( string key )
    {
        this.RemoveLocalItem( key );
        this.UnderlyingBackend.RemoveItem( key );
        this.CompleteRemoval( key );
    }

    /// <inheritdoc />
    protected override async ValueTask RemoveItemAsyncCore( string key, CancellationToken cancellationToken )
    {
        this.RemoveLocalItem( key );
        await this.UnderlyingBackend.RemoveItemAsync( key, cancellationToken );
        this.CompleteRemoval( key );
    }

    /// <summary>
    /// Marks the pending reads of a key, then removes the item from the local cache. When the remote cache is not
    /// blocking, the item is replaced with a replacement value for the transition period.
    /// </summary>
    private void RemoveLocalItem( string key )
    {
        this.InvalidateReadThroughs( key );

        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            this.LocalCache.RemoveItem( key );
        }
        else
        {
            this.LocalCache.RemoveItemImpl( key, new RemovedValue( this.GetTimestamp() ), this.TimeProvider.GetUtcNow() + this._removedItemTransitionPeriod );
        }
    }

    /// <summary>
    /// Marks the pending reads of a key and removes the item from the local cache again, after a blocking remote cache
    /// has removed it. This removes the item that a read has copied from the remote cache between the first local
    /// removal and the remote removal.
    /// </summary>
    private void CompleteRemoval( string key )
    {
        if ( this.UnderlyingBackend.SupportedFeatures.Blocking )
        {
            this.InvalidateReadThroughs( key );
            this.LocalCache.RemoveItemImpl( key );
        }
    }

    /// <param name="options"></param>
    /// <inheritdoc />
    protected override void ClearCore( ClearCacheOptions options )
    {
        this.ClearLocalCache();

        if ( options == ClearCacheOptions.Default )
        {
            this.UnderlyingBackend.Clear();
            this.ClearLocalCache();
        }
    }

    /// <inheritdoc />
    protected override async ValueTask ClearAsyncCore( ClearCacheOptions options, CancellationToken cancellationToken )
    {
        this.ClearLocalCache();

        if ( options == ClearCacheOptions.Default )
        {
            await this.UnderlyingBackend.ClearAsync( options, cancellationToken );
            this.ClearLocalCache();
        }
    }

    /// <summary>
    /// Marks every pending read, then clears the local cache.
    /// </summary>
    private void ClearLocalCache()
    {
        this.InvalidateReadThroughs( null );
        this.LocalCache.Clear();
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
    /// Gets the current instant, in UTC ticks, from the clock of the backend. The value is serialized with every
    /// <see cref="MaterializedCacheItem"/> sent to the remote cache and compared, possibly on another machine, with the
    /// timestamp of a <see cref="RemovedValue"/>. The comparison is therefore only meaningful when the clocks of all
    /// machines sharing the remote cache are synchronized.
    /// </summary>
    /// <summary>
    /// Gets the timestamp of a new remote item or of a local removal. It is read from <see cref="CachingBackend.Clock"/>,
    /// which is the clock of the remote backend, so that the timestamps written by different machines are comparable when
    /// the remote backend uses a shared clock.
    /// </summary>
    /// <returns>The current time of the clock, in UTC ticks.</returns>
    internal long GetTimestamp() => this.Clock.GetCurrentTime().UtcTicks;

    private MaterializedCacheItem Materialize( CacheItem item ) => new( item, this.TimeProvider, this.GetTimestamp() );

    private sealed class Features : CachingBackendEnhancerFeatures
    {
        public Features( CachingBackendFeatures underlyingBackendFeatures ) : base( underlyingBackendFeatures ) { }

        public override bool ContainsDependency => this.UnderlyingBackendFeatures is { ContainsDependency: true, Blocking: true };
    }

    /// <summary>
    /// The registration of a read that reads an item from the remote cache and copies it into the local cache.
    /// </summary>
    private sealed class ReadThrough
    {
        private volatile bool _isInvalidated;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReadThrough"/> class.
        /// </summary>
        /// <param name="key">The key of the item that is read.</param>
        public ReadThrough( string key )
        {
            this.Key = key;
        }

        /// <summary>
        /// Gets the key of the item that is read.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets or sets a value indicating whether a removal, an invalidation or a clearing has run since the read
        /// started, so that the read must withdraw the copy that it stores in the local cache.
        /// </summary>
        public bool IsInvalidated
        {
            get => this._isInvalidated;
            set => this._isInvalidated = value;
        }
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
