// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.Serializers;
using Metalama.Testing.Hooks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IO;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Globalization;
using CacheItemPriority = Microsoft.Extensions.Caching.Memory.CacheItemPriority;
using MemoryCache = Microsoft.Extensions.Caching.Memory.MemoryCache;
using PSCacheItem = Metalama.Patterns.Caching.Implementation.CacheItem;
using PSCacheItemPriority = Metalama.Patterns.Caching.Implementation.CacheItemPriority;

namespace Metalama.Patterns.Caching.Backends;

/// <summary>
/// A <see cref="CachingBackend"/> based on Microsoft.Extensions.Caching.Memory.IMemoryCache (<see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>).
/// This cache is recommended for ASP.NET Core use (as opposed to <c>System.Runtime.Caching.MemoryCache</c>).
/// </summary>
/// <remarks>
/// This backend converts Metalama configuration properties into <see cref="ICacheEntry"/> instances as follows:
/// <list type="bullet">
///    <item>The priority <c>Default</c> is converted to <see cref="CacheItemPriority.Normal"/>.</item>
///    <item>The property <see cref="ICacheEntry.Size"/> is normally not set. If you want it to be set, supply a function to calculate this value for each entry
/// in the <see cref="MemoryCachingBackend"/> constructor. You only need to do this if you intend to limit the size
/// of the cache.</item>
/// </list>
/// <para>
/// Each instance prefixes its keys with an identifier of its own, so several instances that share one
/// <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> keep separate items and separate dependencies. This
/// is what the two layers of a layered backend need when the application registers a single
/// <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> in its service container.
/// </para>
/// </remarks>
[PublicAPI]
internal partial class MemoryCachingBackend : CachingBackend
{
    private readonly IMemoryCache _cache;
    private readonly Func<object?, long> _sizeCalculator;
    private readonly ICachingSerializer? _serializer;
    private readonly string _itemKeyPrefix;
    private readonly ITestSynchronizationProvider? _testSynchronizationProvider;
    private static readonly RecyclableMemoryStreamManager _memoryStreamManager = new();

    /// <summary>
    /// The value of <see cref="MemoryCacheItem.Sync"/> in the tombstones that the backend stores.
    /// </summary>
    private static readonly object _tombstoneSync = new();

    /// <summary>
    /// The locks of the keys that an operation currently uses. Every operation that changes the item of a key, or the
    /// registrations of that key in the backward dependency index, holds the lock of the key.
    /// </summary>
    private readonly ConcurrentDictionary<string, KeyLock> _keyLocks = new( StringComparer.Ordinal );

    /// <summary>
    /// A lock that <see cref="ClearCore"/> acquires for writing and that every operation acquires for reading together
    /// with the lock of its key, so that a clearing never runs between the registration of a key in the backward
    /// dependency index and the store of its value. Recursion is supported because the post-eviction callbacks of some
    /// <see cref="IMemoryCache"/> implementations run on the thread that clears the cache.
    /// </summary>
    private readonly ReaderWriterLockSlim _clearLock = new( LockRecursionPolicy.SupportsRecursion );

    /// <summary>
    /// The backward dependency index: for each dependency key, the keys of the items that declare it in their forward
    /// dependencies (<see cref="CacheItem.Dependencies"/>). A backward dependency set is locked only for one change or
    /// one copy, and no other lock is acquired while it is held.
    /// </summary>
    private readonly ConcurrentDictionary<string, BackwardDependencySet> _backwardDependencies = new( StringComparer.Ordinal );

    /// <summary>
    /// The identifier given to the last instance of the <see cref="MemoryCachingBackend"/> class.
    /// </summary>
    private static int _lastInstanceId;

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCachingBackend"/> class based on a new instance of the <see cref="Microsoft.Extensions.Caching.Memory.MemoryCache"/> class.
    /// </summary>
    internal MemoryCachingBackend( MemoryCachingBackendConfiguration? configuration = null, IServiceProvider? serviceProvider = null ) : this(
        null,
        configuration,
        serviceProvider ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCachingBackend"/> class based on the given <see cref="IMemoryCache"/>. The backend creates cache entries
    /// with size calculated by the given function.
    /// </summary>
    /// <param name="cache">An <see cref="IMemoryCache"/>.</param>
    /// <param name="configuration"></param>
    /// <param name="serviceProvider"></param>
    internal MemoryCachingBackend(
        IMemoryCache? cache,
        MemoryCachingBackendConfiguration? configuration = null,
        IServiceProvider? serviceProvider = null ) : base(
        configuration,
        serviceProvider )
    {
        configuration ??= new MemoryCachingBackendConfiguration();
        this._cache = cache ?? (IMemoryCache?) serviceProvider?.GetService( typeof(IMemoryCache) ) ?? new MemoryCache( new MemoryCacheOptions() );
        this._serializer = configuration.Serializer;
        this._sizeCalculator = this._serializer != null ? item => ((byte[]?) item)?.Length ?? 0 : configuration.SizeCalculator;

        var instanceId = Interlocked.Increment( ref _lastInstanceId ).ToString( CultureInfo.InvariantCulture );
        this._itemKeyPrefix = nameof(MemoryCachingBackend) + ":" + instanceId + ":item:";
        this._testSynchronizationProvider = (ITestSynchronizationProvider?) serviceProvider?.GetService( typeof(ITestSynchronizationProvider) );
    }

    /// <summary>
    /// Blocks the current thread at a synchronization point when a test has registered an
    /// <see cref="ITestSynchronizationProvider"/> and has enabled a synchronization point of this name. When no provider
    /// is registered, the method only performs a null check.
    /// </summary>
    private void SyncPoint( string name ) => this._testSynchronizationProvider?.SyncPoint( name );

    private string GetItemKey( string key )
    {
        return this._itemKeyPrefix + key;
    }

    private static CacheItemRemovedReason CreateRemovalReason( EvictionReason sourceReason )
    {
        switch ( sourceReason )
        {
            case EvictionReason.None:
            case EvictionReason.Replaced:
                return CacheItemRemovedReason.Other;

            case EvictionReason.TokenExpired:
                return CacheItemRemovedReason.Invalidated;

            case EvictionReason.Capacity:
                return CacheItemRemovedReason.Evicted;

            case EvictionReason.Expired:
                return CacheItemRemovedReason.Expired;

            case EvictionReason.Removed:
                return CacheItemRemovedReason.Removed;

            default:
                throw new ArgumentException( string.Format( CultureInfo.InvariantCulture, "The CacheEntryRemovedReason '{0}' is unknown.", sourceReason ) );
        }
    }

    private MemoryCacheEntryOptions CreatePolicy( PSCacheItem item, object? value )
    {
        var targetPolicy = new MemoryCacheEntryOptions();
        targetPolicy.RegisterPostEvictionCallback( this.OnCacheItemRemoved );

        if ( item.Configuration != null )
        {
            if ( item.Configuration.AbsoluteExpiration.HasValue )
            {
                targetPolicy.AbsoluteExpirationRelativeToNow = item.Configuration.AbsoluteExpiration.Value;
            }

            if ( item.Configuration.SlidingExpiration.HasValue )
            {
                targetPolicy.SlidingExpiration = item.Configuration.SlidingExpiration.Value;
            }

            switch ( item.Configuration.Priority.GetValueOrDefault() )
            {
                case PSCacheItemPriority.Default:
                    targetPolicy.Priority = CacheItemPriority.Normal;

                    break;

                case PSCacheItemPriority.Low:
                    targetPolicy.Priority = CacheItemPriority.Low;

                    break;

                case PSCacheItemPriority.High:
                    targetPolicy.Priority = CacheItemPriority.High;

                    break;

                case PSCacheItemPriority.NotRemovable:
                    targetPolicy.Priority = CacheItemPriority.NeverRemove;

                    break;

                default:
                    throw new NotSupportedException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "The priority '{0}' is not supported by the MemoryCache back-end.",
                            item.Configuration.Priority ) );
            }
        }

        targetPolicy.Size = this._sizeCalculator( value );

        return targetPolicy;
    }

    private void OnCacheItemRemoved( object keyAsObject, object? value, EvictionReason reason, object? state )
    {
        if ( reason is EvictionReason.Removed or EvictionReason.Replaced )
        {
            // In this case, all actions are taken by the method that removes the item.
            return;
        }

        var fullKey = (string) keyAsObject;

        if ( !fullKey.StartsWith( this._itemKeyPrefix, StringComparison.Ordinal ) || value is not MemoryCacheItem evictedItem )
        {
            return;
        }

        var key = fullKey.Substring( this._itemKeyPrefix.Length );
        MemoryCacheItem? currentItem;
        var keyLock = this.EnterKeyLock( key );

        try
        {
            // The callback runs on the thread pool, possibly after a newer value has been stored under the same key. Only
            // the dependencies that the newer value does not declare are unregistered, and no event is raised.
            currentItem = this.GetStoredItem( fullKey );
            this.RemoveBackwardDependencies( key, evictedItem.Dependencies, currentItem );
        }
        finally
        {
            this.ExitKeyLock( key, keyLock );
        }

        if ( currentItem == null )
        {
            this.OnItemRemoved( key, CreateRemovalReason( reason ), this.Id );
        }
    }

    /// <inheritdoc />
    protected override void SetItemCore( string key, PSCacheItem item )
    {
        // The serializer and the size calculator are foreign code. They run before the lock of the key is acquired and
        // before any state is changed, so that an exception leaves the backend unchanged.
        var cacheValue = this.Serialize( new MemoryCacheItem( item.Value, item.Dependencies, new object() ) );
        var policy = this.CreatePolicy( item, cacheValue.Value );
        var itemKey = this.GetItemKey( key );
        var keyLock = this.EnterKeyLock( key );

        try
        {
            var previousValue = this.GetStoredItem( itemKey );

            // The new dependencies are registered before the value is stored. Afterwards, only the dependencies that the
            // new value does not declare are unregistered, so that the key stays registered under a dependency that both
            // values declare.
            if ( !cacheValue.Dependencies.IsDefaultOrEmpty )
            {
                foreach ( var dependency in cacheValue.Dependencies )
                {
                    this.AddBackwardDependency( dependency, key );
                }
            }

            this._cache.Set( itemKey, cacheValue, policy );

            if ( previousValue != null )
            {
                this.RemoveBackwardDependencies( key, previousValue.Dependencies, cacheValue );
            }
        }
        finally
        {
            this.ExitKeyLock( key, keyLock );
        }
    }

    /// <inheritdoc />
    protected override bool ContainsItemCore( string key )
    {
        return this._cache.Get( this.GetItemKey( key ) ) != null;
    }

    /// <inheritdoc />  
    protected override CacheItem? GetItemCore( string key, bool includeDependencies )
    {
        return this.Deserialize( (MemoryCacheItem?) this._cache.Get( this.GetItemKey( key ) ) );
    }

    protected MemoryCacheItem? Deserialize( MemoryCacheItem? item )
    {
        if ( item == null )
        {
            return null;
        }
        else if ( item.Value == null )
        {
            return item;
        }
        else if ( this._serializer != null )
        {
            var stream = new MemoryStream( (byte[]) item.Value!, false );
            var reader = new BinaryReader( stream );

            return item with { Value = this._serializer.Deserialize( reader ) };
        }
        else
        {
            return item;
        }
    }

    protected MemoryCacheItem Serialize( MemoryCacheItem item )
    {
        if ( this._serializer != null )
        {
            using var stream = _memoryStreamManager.GetStream();
            var writer = new BinaryWriter( stream );
            this._serializer.Serialize( item.Value!, writer );

            return item with { Value = stream.ToArray() };
        }
        else
        {
            return item;
        }
    }

    /// <inheritdoc />
    protected override void InvalidateDependencyCore( string key ) => this.InvalidateDependencyImpl( key );

    internal void InvalidateDependencyImpl( string key, MemoryCacheItem? replacementValue = null, DateTimeOffset? replacementValueExpiration = null )
        => this.InvalidateDependencyImpl( key, replacementValue, replacementValueExpiration, new HashSet<string>( StringComparer.Ordinal ) );

    private void InvalidateDependencyImpl(
        string key,
        MemoryCacheItem? replacementValue,
        DateTimeOffset? replacementValueExpiration,
        HashSet<string> invalidatedKeys )
    {
        // The backward dependency set is copied under its lock, and the lock is released before the items are removed, so
        // that no thread acquires the lock of a key while it holds the lock of a backward dependency set.
        var dependents = this.GetDependents( key );

        this.SyncPoint( "MemoryCachingBackend.InvalidateDependencyImpl:DependentsCopied" );

        foreach ( var item in dependents )
        {
            if ( this.RemoveItemImpl( item, replacementValue, replacementValueExpiration, key ) )
            {
                // Recursively invalidate items that depend on this item. The set of invalidated keys stops the recursion
                // on a cyclic dependency graph.
                if ( invalidatedKeys.Add( item ) )
                {
                    this.InvalidateDependencyImpl( item, replacementValue, replacementValueExpiration, invalidatedKeys );
                }

                this.OnItemRemoved( item, CacheItemRemovedReason.Invalidated, this.Id );
            }
        }

        this.OnDependencyInvalidated( key, this.Id );
    }

    /// <summary>
    /// Removes the item of a key, or replaces it with a replacement value (tombstone) for a limited time.
    /// </summary>
    /// <param name="key">The key of the item.</param>
    /// <param name="replacementValue">The replacement value, or <see langword="null"/> to remove the item.</param>
    /// <param name="replacementValueExpiration">The instant, read from the clock of the backend, at which the replacement value expires.</param>
    /// <param name="invalidatedDependency">
    /// The dependency whose invalidation removes the item, or <see langword="null"/>. When it is set, a value that does not
    /// declare this dependency, because a concurrent <see cref="CachingBackend.SetItem"/> has stored it, is kept.
    /// </param>
    /// <returns><see langword="true"/> when a value has been removed or replaced.</returns>
    internal bool RemoveItemImpl(
        string key,
        MemoryCacheItem? replacementValue = null,
        DateTimeOffset? replacementValueExpiration = null,
        string? invalidatedDependency = null )
    {
        if ( replacementValue != null && replacementValueExpiration == null )
        {
            throw new ArgumentException(
                "If " + nameof(replacementValue) + " is specified, " + nameof(replacementValueExpiration) + " must also be specified." );
        }

        var itemKey = this.GetItemKey( key );
        var keyLock = this.EnterKeyLock( key );

        try
        {
            this.SyncPoint( "MemoryCachingBackend.RemoveItemImpl:ItemLocked" );

            var cacheValue = this.GetStoredItem( itemKey );

            if ( !IsLive( cacheValue ) || (invalidatedDependency != null && !Declares( cacheValue, invalidatedDependency )) )
            {
                if ( invalidatedDependency != null && !Declares( cacheValue, invalidatedDependency ) )
                {
                    // The registration does not correspond to the current value, if any.
                    this.RemoveBackwardDependency( invalidatedDependency, key );
                }

                // The tombstone masks the value that another layer may still hold for the key. A direct removal replaces an
                // existing tombstone, so that the tombstone carries the timestamp and the lifetime of the latest removal. An
                // invalidation does not replace it, because its copy of the backward dependency set may be older than the
                // removal that wrote the tombstone.
                if ( replacementValue != null && (cacheValue == null || invalidatedDependency == null) )
                {
                    this.StoreTombstone( itemKey, replacementValue, replacementValueExpiration!.Value );
                }

                return false;
            }

            if ( replacementValue == null )
            {
                this._cache.Remove( itemKey );
            }
            else
            {
                this.StoreTombstone( itemKey, replacementValue, replacementValueExpiration!.Value );
            }

            this.RemoveBackwardDependencies( key, cacheValue!.Dependencies, null );

            return true;
        }
        finally
        {
            this.ExitKeyLock( key, keyLock );
        }
    }

    /// <summary>
    /// Stores a replacement value (tombstone). Must be called while the lock of the key is held.
    /// </summary>
    /// <remarks>
    /// The expiration is converted to a duration with the clock of the backend, because the <see cref="IMemoryCache"/>
    /// evaluates it with its own clock. The tombstone is never evicted for capacity before it expires, and it has a size,
    /// as a size-limited <see cref="MemoryCache"/> requires.
    /// </remarks>
    private void StoreTombstone( string itemKey, MemoryCacheItem replacementValue, DateTimeOffset expiration )
    {
        var lifetime = expiration - this.TimeProvider.GetUtcNow();

        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = lifetime > TimeSpan.Zero ? lifetime : TimeSpan.FromTicks( 1 ),
            Priority = CacheItemPriority.NeverRemove,
            Size = 0
        };

        this._cache.Set( itemKey, replacementValue with { Sync = _tombstoneSync }, options );
    }

    private MemoryCacheItem? GetStoredItem( string itemKey ) => (MemoryCacheItem?) this._cache.Get( itemKey );

    /// <summary>
    /// Determines whether an entry holds a value, as opposed to a tombstone.
    /// </summary>
    private static bool IsLive( MemoryCacheItem? item ) => item != null && !ReferenceEquals( item.Sync, _tombstoneSync );

    private static bool Declares( MemoryCacheItem? item, string dependency )
        => IsLive( item ) && !item!.Dependencies.IsDefaultOrEmpty && item.Dependencies.Contains( dependency );

    /// <summary>
    /// Acquires the lock of a key. The lock does not depend on the stored value, so two operations on the same key always
    /// exclude each other.
    /// </summary>
    private KeyLock EnterKeyLock( string key )
    {
        this._clearLock.EnterReadLock();

        while ( true )
        {
            var keyLock = this._keyLocks.GetOrAdd( key, _ => new KeyLock() );
            Interlocked.Increment( ref keyLock.ReferenceCount );
            Monitor.Enter( keyLock );

            if ( !keyLock.IsRetired )
            {
                return keyLock;
            }

            // The lock was removed from the dictionary after this thread read it.
            Interlocked.Decrement( ref keyLock.ReferenceCount );
            Monitor.Exit( keyLock );
        }
    }

    /// <summary>
    /// Releases the lock of a key, and removes it from the dictionary when no other operation uses it.
    /// </summary>
    private void ExitKeyLock( string key, KeyLock keyLock )
    {
        if ( Interlocked.Decrement( ref keyLock.ReferenceCount ) == 0 )
        {
            keyLock.IsRetired = true;
            ((ICollection<KeyValuePair<string, KeyLock>>) this._keyLocks).Remove( new KeyValuePair<string, KeyLock>( key, keyLock ) );
        }

        Monitor.Exit( keyLock );
        this._clearLock.ExitReadLock();
    }

    /// <summary>
    /// Registers a key in the backward dependency set of a dependency. A set that has been removed from the index is never used
    /// again, so that no key is registered in a set that no lookup can reach.
    /// </summary>
    private void AddBackwardDependency( string dependency, string key )
    {
        while ( true )
        {
            var backwardDependencySet = this._backwardDependencies.GetOrAdd( dependency, _ => new BackwardDependencySet() );

            this.SyncPoint( "MemoryCachingBackend.AddBackwardDependency:DependencySetRead" );

            lock ( backwardDependencySet )
            {
                if ( !backwardDependencySet.IsRemoved )
                {
                    backwardDependencySet.DependentKeys.Add( key );

                    return;
                }
            }
        }
    }

    /// <summary>
    /// Removes a key from the backward dependency set of a dependency, and removes this instance of the set from the index when it
    /// becomes empty.
    /// </summary>
    private void RemoveBackwardDependency( string dependency, string key )
    {
        if ( !this._backwardDependencies.TryGetValue( dependency, out var backwardDependencySet ) )
        {
            return;
        }

        this.SyncPoint( "MemoryCachingBackend.RemoveBackwardDependency:DependencySetRead" );

        lock ( backwardDependencySet )
        {
            if ( !backwardDependencySet.IsRemoved && backwardDependencySet.DependentKeys.Remove( key ) && backwardDependencySet.DependentKeys.Count == 0 )
            {
                backwardDependencySet.IsRemoved = true;

                ((ICollection<KeyValuePair<string, BackwardDependencySet>>) this._backwardDependencies).Remove(
                    new KeyValuePair<string, BackwardDependencySet>( dependency, backwardDependencySet ) );
            }
        }
    }

    /// <summary>
    /// Removes a key from the backward dependency sets of the given forward dependencies of an item, except the
    /// dependencies that <paramref name="keptItem"/> declares.
    /// </summary>
    private void RemoveBackwardDependencies( string key, ImmutableArray<string> forwardDependencies, MemoryCacheItem? keptItem )
    {
        if ( forwardDependencies.IsDefaultOrEmpty )
        {
            return;
        }

        foreach ( var dependency in forwardDependencies )
        {
            if ( !Declares( keptItem, dependency ) )
            {
                this.RemoveBackwardDependency( dependency, key );
            }
        }
    }

    /// <summary>
    /// Copies the keys registered in the backward dependency set of a dependency.
    /// </summary>
    private List<string> GetDependents( string dependency )
    {
        if ( !this._backwardDependencies.TryGetValue( dependency, out var backwardDependencySet ) )
        {
            return new List<string>();
        }

        lock ( backwardDependencySet )
        {
            this.SyncPoint( "MemoryCachingBackend.InvalidateDependencyImpl:DependencyLocked" );

            return backwardDependencySet.IsRemoved ? new List<string>() : backwardDependencySet.DependentKeys.ToList();
        }
    }

    /// <inheritdoc />
    protected override bool ContainsDependencyCore( string key )
    {
        if ( !this._backwardDependencies.TryGetValue( key, out var backwardDependencySet ) )
        {
            return false;
        }

        lock ( backwardDependencySet )
        {
            return !backwardDependencySet.IsRemoved && backwardDependencySet.DependentKeys.Count > 0;
        }
    }

    /// <param name="options"></param>
    /// <inheritdoc />
    protected override void ClearCore( ClearCacheOptions options )
    {
        this.SyncPoint( "MemoryCachingBackend.ClearCore:ClearRequested" );

        this._clearLock.EnterWriteLock();

        try
        {
            this.ClearCacheAndIndex( options );
        }
        finally
        {
            this._clearLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Clears or compacts the <see cref="IMemoryCache"/> and clears the backward dependency index. Must be called while
    /// the write lock of <see cref="_clearLock"/> is held.
    /// </summary>
    private void ClearCacheAndIndex( ClearCacheOptions options )
    {
        switch ( this._cache )
        {
            case MemoryCache classicMemoryCache when (options & ClearCacheOptions.Compact) != 0:
                classicMemoryCache.Compact( 1 );

                break;

            case MemoryCache classicMemoryCache:
                classicMemoryCache.Clear();

                break;

            case IClearableMemoryCache clearableMemoryCache when (options & ClearCacheOptions.Compact) != 0:
                clearableMemoryCache.Compact( 1 );

                break;

            case IClearableMemoryCache clearableMemoryCache:
                clearableMemoryCache.Clear();

                break;

            default:
                throw new NotSupportedException( "IMemoryCache implementations other than MemoryCache and IClearableMemoryCache do not support clearing." );
        }

        // The backward dependency index is not stored in the IMemoryCache. After a compaction, the post-eviction callbacks
        // unregister the evicted items.
        if ( (options & ClearCacheOptions.Compact) == 0 )
        {
            this._backwardDependencies.Clear();
        }
    }

    /// <inheritdoc />
    protected override void RemoveItemCore( string key )
    {
        if ( this.RemoveItemImpl( key ) )
        {
            this.OnItemRemoved( key, CacheItemRemovedReason.Removed, this.Id );
        }
    }

    /// <inheritdoc />
    protected override CachingBackendFeatures CreateFeatures()
    {
        return new Features( this._cache is MemoryCache or IClearableMemoryCache );
    }

    /// <inheritdoc />
    protected override void DisposeCore( bool disposing, CancellationToken cancellationToken )
    {
        base.DisposeCore( disposing, cancellationToken );
        this._cache.Dispose();
        this._clearLock.Dispose();
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore( CancellationToken cancellationToken )
    {
        await base.DisposeAsyncCore( cancellationToken ).ConfigureAwait( false );
        this._cache.Dispose();
        this._clearLock.Dispose();
    }
}