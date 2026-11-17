// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using Metalama.Patterns.Caching.Implementation;
using Metalama.Patterns.Caching.Serializers;
using SharpCrafters.Common.Testing.Hooks;
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
/// <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> in its service container. <see cref="CachingBackend.Clear"/>
/// removes only the items of the current instance, one key at a time, so it is supported on any
/// <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>. The instance disposes the
/// <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/> only when it has created it.
/// </para>
/// <para>
/// Thread safety rests on three rules. Every operation that changes the item of a key, or the registrations of that key
/// in the backward dependency index, holds the lock of that key, which does not depend on the stored value. The dependency
/// index is owned by the instance, and a backward dependency set is locked only for the duration of one change or one copy,
/// never while another lock is acquired. Foreign code (the serializer and the size calculator) runs before any lock is
/// acquired.
/// </para>
/// </remarks>
[PublicAPI]
internal partial class MemoryCachingBackend : CachingBackend
{
    private readonly IMemoryCache _cache;
    private readonly bool _ownsCache;
    private readonly Func<object?, long> _sizeCalculator;
    private readonly ICachingSerializer? _serializer;
    private readonly string _itemKeyPrefix;

    /// <summary>
    /// The optional test synchronization service, resolved once from the service provider. It is never registered in
    /// production, in which case it stays <see langword="null"/> and every synchronization point is skipped. It is resolved
    /// per instance, rather than stored in global state, so that concurrent tests cannot interfere with each other.
    /// </summary>
    private readonly ITestSynchronizationProvider? _testSynchronizationProvider;
    private static readonly RecyclableMemoryStreamManager _memoryStreamManager = new();

    /// <summary>
    /// The locks of the keys that an operation currently uses. An entry is removed when no operation uses it.
    /// </summary>
    private readonly ConcurrentDictionary<string, KeyLock> _keyLocks = new( StringComparer.Ordinal );

    /// <summary>
    /// The backward dependency index: for each dependency key, the keys of the items that declare it in their forward
    /// dependencies (<see cref="CacheItem.Dependencies"/>).
    /// </summary>
    private readonly ConcurrentDictionary<string, BackwardDependencySet> _backwardDependencies = new( StringComparer.Ordinal );

    /// <summary>
    /// The forward dependencies of the items that have been removed or evicted while other items still depended on their
    /// key. Such a key stays in the backward dependency sets of these forward dependencies, so that an invalidation of one
    /// of them still reaches the items that depend on the removed item. The entry of a key is released when no item that
    /// is still in the cache depends on the key, directly or through the keys of other removed items.
    /// </summary>
    /// <remarks>
    /// A key that is registered in a backward dependency set and that has no entry in this dictionary is the key of an
    /// item that is in the cache, or of an item that <see cref="CachingBackend.SetItem"/> is storing.
    /// </remarks>
    private readonly ConcurrentDictionary<string, ImmutableArray<string>> _forwardDependenciesOfRemovedItems = new( StringComparer.Ordinal );

    /// <summary>
    /// The keys for which the current instance has stored an entry in the <see cref="IMemoryCache"/>. The set is used to
    /// clear and to dispose the instance without affecting other users of a shared <see cref="IMemoryCache"/>.
    /// </summary>
    private readonly ConcurrentDictionary<string, bool> _storedKeys = new( StringComparer.Ordinal );

    /// <summary>
    /// The keys of the entries that are copies of items of another layer and that were read without their dependencies
    /// (<see cref="EntryState.HasUnknownDependencies"/>). An invalidation cannot find these entries in the dependency
    /// index, so <see cref="RemoveItemsWithUnknownDependencies"/> removes them all.
    /// </summary>
    private readonly ConcurrentDictionary<string, bool> _keysWithUnknownDependencies = new( StringComparer.Ordinal );

    private volatile bool _isDisposed;

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
    /// <param name="cache">
    /// An <see cref="IMemoryCache"/>, which the caller owns, or <see langword="null"/> to use the <see cref="IMemoryCache"/> of
    /// the service provider, or a new <see cref="MemoryCache"/> when the service provider has none.
    /// </param>
    /// <param name="configuration">The configuration of the backend.</param>
    /// <param name="serviceProvider">The service provider of the backend.</param>
    internal MemoryCachingBackend(
        IMemoryCache? cache,
        MemoryCachingBackendConfiguration? configuration = null,
        IServiceProvider? serviceProvider = null ) : this( cache, false, configuration, serviceProvider ) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MemoryCachingBackend"/> class based on the given <see cref="IMemoryCache"/>,
    /// and specifies whether the backend owns it.
    /// </summary>
    /// <param name="cache">
    /// An <see cref="IMemoryCache"/>, or <see langword="null"/> to use the <see cref="IMemoryCache"/> of the service provider,
    /// or a new <see cref="MemoryCache"/>, which the backend owns, when the service provider has none.
    /// </param>
    /// <param name="ownsCache">
    /// <see langword="true"/> when the backend disposes <paramref name="cache"/> when it is disposed.
    /// </param>
    /// <param name="configuration">The configuration of the backend.</param>
    /// <param name="serviceProvider">The service provider of the backend.</param>
    internal MemoryCachingBackend(
        IMemoryCache? cache,
        bool ownsCache,
        MemoryCachingBackendConfiguration? configuration,
        IServiceProvider? serviceProvider ) : base(
        configuration,
        serviceProvider )
    {
        configuration ??= new MemoryCachingBackendConfiguration();

        if ( cache != null )
        {
            this._cache = cache;
            this._ownsCache = ownsCache;
        }
        else if ( serviceProvider?.GetService( typeof(IMemoryCache) ) is IMemoryCache sharedCache )
        {
            this._cache = sharedCache;
            this._ownsCache = false;
        }
        else
        {
            this._cache = new MemoryCache( new MemoryCacheOptions() );
            this._ownsCache = true;
        }

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

    private string GetItemKey( string key ) => this._itemKeyPrefix + key;

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

    #region Key locks

    /// <summary>
    /// Acquires the lock of a key. The lock does not depend on the value stored for the key, so two operations on the
    /// same key always exclude each other.
    /// </summary>
    private KeyLock EnterKeyLock( string key )
    {
        while ( true )
        {
            var keyLock = this._keyLocks.GetOrAdd( key, _ => new KeyLock() );
            Interlocked.Increment( ref keyLock.ReferenceCount );
            Monitor.Enter( keyLock );

            if ( !keyLock.IsRetired )
            {
                return keyLock;
            }

            // The lock was removed from the dictionary after this thread read it. Another lock object is used.
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
    }

    #endregion

    #region Backward dependency index

    /// <summary>
    /// Registers a key in the backward dependency set of a dependency, creating the set when needed.
    /// </summary>
    private void AddBackwardDependency( string dependencyKey, string key )
    {
        while ( true )
        {
            var backwardDependencySet = this._backwardDependencies.GetOrAdd( dependencyKey, _ => new BackwardDependencySet() );

            this.SyncPoint( SyncPoints.AddBackwardDependencySetRead );

            lock ( backwardDependencySet )
            {
                // A set that has been removed from the index is never used again, so that no key is registered in a
                // set that no lookup can reach.
                if ( !backwardDependencySet.IsRemoved )
                {
                    backwardDependencySet.DependentKeys.Add( key );

                    return;
                }
            }
        }
    }

    /// <summary>
    /// Removes a key from the backward dependency set of a dependency, and removes the set from the index when it becomes
    /// empty. When the dependency is the key of a removed item whose registrations are kept, the dependency is recorded
    /// in <paramref name="releaseCandidates"/>, because these registrations may no longer be needed.
    /// </summary>
    private void RemoveBackwardDependency( string dependencyKey, string key, ref List<string>? releaseCandidates )
    {
        if ( !this._backwardDependencies.TryGetValue( dependencyKey, out var backwardDependencySet ) )
        {
            return;
        }

        this.SyncPoint( SyncPoints.RemoveBackwardDependencySetRead );

        lock ( backwardDependencySet )
        {
            if ( backwardDependencySet.IsRemoved || !backwardDependencySet.DependentKeys.Remove( key ) )
            {
                return;
            }

            if ( backwardDependencySet.DependentKeys.Count == 0 )
            {
                backwardDependencySet.IsRemoved = true;

                // Only this instance of the set is removed. A newer set stored under the same key is kept.
                ((ICollection<KeyValuePair<string, BackwardDependencySet>>) this._backwardDependencies).Remove(
                    new KeyValuePair<string, BackwardDependencySet>( dependencyKey, backwardDependencySet ) );
            }
        }

        if ( this._forwardDependenciesOfRemovedItems.ContainsKey( dependencyKey ) )
        {
            (releaseCandidates ??= new List<string>()).Add( dependencyKey );
        }
    }

    /// <summary>
    /// Copies the keys registered in the backward dependency set of a dependency.
    /// </summary>
    private List<string> GetDependents( string dependencyKey )
    {
        if ( !this._backwardDependencies.TryGetValue( dependencyKey, out var backwardDependencySet ) )
        {
            return new List<string>();
        }

        lock ( backwardDependencySet )
        {
            this.SyncPoint( SyncPoints.DependencyLocked );

            return backwardDependencySet.IsRemoved ? new List<string>() : backwardDependencySet.DependentKeys.ToList();
        }
    }

    /// <summary>
    /// Determines whether at least one key is registered in the backward dependency set of a dependency.
    /// </summary>
    private bool HasDependents( string dependencyKey )
    {
        if ( !this._backwardDependencies.TryGetValue( dependencyKey, out var backwardDependencySet ) )
        {
            return false;
        }

        lock ( backwardDependencySet )
        {
            return !backwardDependencySet.IsRemoved && backwardDependencySet.DependentKeys.Count > 0;
        }
    }

    /// <summary>
    /// Determines whether at least one item that is in the cache depends on a key, directly or through the keys of
    /// removed items whose registrations are kept.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The registrations kept for a removed item are useful only while such an item exists. A key that is registered
    /// under <paramref name="key"/> and that is itself the key of a removed item does not count. The search continues
    /// with the keys registered under that key. The removed items of a dependency cycle therefore do not keep the
    /// registrations of each other.
    /// </para>
    /// <para>
    /// The item of <paramref name="key"/> itself is removed, or is being removed, when this method is called, so a
    /// registration of <paramref name="key"/> under itself does not count either.
    /// </para>
    /// </remarks>
    private bool HasLiveDependents( string key )
    {
        HashSet<string>? visitedKeys = null;
        Stack<string>? pendingKeys = null;
        var currentKey = key;

        while ( true )
        {
            if ( this._backwardDependencies.TryGetValue( currentKey, out var backwardDependencySet ) )
            {
                lock ( backwardDependencySet )
                {
                    if ( !backwardDependencySet.IsRemoved )
                    {
                        foreach ( var dependentKey in backwardDependencySet.DependentKeys )
                        {
                            if ( string.Equals( dependentKey, key, StringComparison.Ordinal ) )
                            {
                                continue;
                            }

                            if ( !this._forwardDependenciesOfRemovedItems.ContainsKey( dependentKey ) )
                            {
                                return true;
                            }

                            visitedKeys ??= new HashSet<string>( StringComparer.Ordinal );

                            if ( visitedKeys.Add( dependentKey ) )
                            {
                                (pendingKeys ??= new Stack<string>()).Push( dependentKey );
                            }
                        }
                    }
                }
            }

            if ( pendingKeys == null || pendingKeys.Count == 0 )
            {
                return false;
            }

            currentKey = pendingKeys.Pop();
        }
    }

    /// <summary>
    /// Removes a key from the backward dependency sets of several dependencies, except the dependencies listed in
    /// <paramref name="keptDependencyKeys"/>. Must be called while the lock of the key is held.
    /// </summary>
    private void RemoveBackwardDependencies(
        string key,
        ImmutableArray<string> dependencyKeys,
        ImmutableArray<string> keptDependencyKeys,
        ref List<string>? releaseCandidates )
    {
        foreach ( var dependencyKey in dependencyKeys )
        {
            if ( keptDependencyKeys.IsEmpty || !keptDependencyKeys.Contains( dependencyKey ) )
            {
                this.RemoveBackwardDependency( dependencyKey, key, ref releaseCandidates );
            }
        }
    }

    /// <summary>
    /// Gets the dependencies under which a key is registered in the backward dependency index: the forward dependencies
    /// of its live item, and the forward dependencies kept after the removal of a previous item. Must be called while the lock of the key is held.
    /// </summary>
    private ImmutableArray<string> GetRegisteredDependencies( string key, MemoryCacheItem? item )
    {
        var itemDependencies = IsLive( item ) && !item!.Dependencies.IsDefaultOrEmpty ? item.Dependencies : ImmutableArray<string>.Empty;

        if ( !this._forwardDependenciesOfRemovedItems.TryGetValue( key, out var keptDependencies ) || keptDependencies.IsEmpty )
        {
            return itemDependencies;
        }

        return itemDependencies.IsEmpty ? keptDependencies : itemDependencies.Union( keptDependencies, StringComparer.Ordinal ).ToImmutableArray();
    }

    /// <summary>
    /// Releases the registrations kept for the removed items that no item in the cache depends on any more. Must be
    /// called while no key lock is held.
    /// </summary>
    /// <param name="releaseCandidates">
    /// The keys of removed items from whose backward dependency set a key has been removed, or <see langword="null"/>.
    /// </param>
    private void ReleaseRegistrationsOfRemovedItems( List<string>? releaseCandidates )
    {
        if ( releaseCandidates == null )
        {
            return;
        }

        var pendingKeys = new Queue<string>( releaseCandidates );

        while ( pendingKeys.Count > 0 )
        {
            var key = pendingKeys.Dequeue();

            if ( !this._forwardDependenciesOfRemovedItems.ContainsKey( key ) )
            {
                continue;
            }

            List<string>? newReleaseCandidates = null;
            var keyLock = this.EnterKeyLock( key );

            try
            {
                if ( this.HasLiveDependents( key ) || !this._forwardDependenciesOfRemovedItems.TryRemove( key, out var forwardDependencies ) )
                {
                    continue;
                }

                this.RemoveBackwardDependencies( key, forwardDependencies, ImmutableArray<string>.Empty, ref newReleaseCandidates );
            }
            finally
            {
                this.ExitKeyLock( key, keyLock );
            }

            if ( newReleaseCandidates != null )
            {
                foreach ( var candidate in newReleaseCandidates )
                {
                    pendingKeys.Enqueue( candidate );
                }
            }
        }
    }

    #endregion

    /// <summary>
    /// Reads the entry stored for an item key.
    /// </summary>
    private MemoryCacheItem? GetStoredItem( string itemKey ) => (MemoryCacheItem?) this._cache.Get( itemKey );

    /// <summary>
    /// Determines whether an entry holds a value, as opposed to the replacement value (tombstone) that a layered backend
    /// stores when it removes an item.
    /// </summary>
    private static bool IsLive( MemoryCacheItem? item ) => item != null && item.State is not EntryState { IsReplacement: true };

    /// <summary>
    /// Records that an entry has been removed, replaced or evicted by the current instance, so that its post-eviction
    /// callback does nothing. Must be called while the lock of the key is held.
    /// </summary>
    private static void Detach( MemoryCacheItem? item )
    {
        if ( item?.State is EntryState entryState )
        {
            entryState.IsDetached = true;
        }
    }

    /// <summary>
    /// Reverts <see cref="Detach"/> for an entry that is still stored, so that the backend handles its later eviction.
    /// Must be called while the lock of the key is held.
    /// </summary>
    private static void Attach( MemoryCacheItem? item )
    {
        if ( item?.State is EntryState entryState )
        {
            entryState.IsDetached = false;
        }
    }

    private void OnCacheItemRemoved( object keyAsObject, object? value, EvictionReason reason, object? state )
    {
        if ( this._isDisposed || keyAsObject is not string fullKey || !fullKey.StartsWith( this._itemKeyPrefix, StringComparison.Ordinal ) )
        {
            return;
        }

        if ( value is not MemoryCacheItem { State: EntryState entryState } evictedItem )
        {
            return;
        }

        var key = fullKey.Substring( this._itemKeyPrefix.Length );
        var raiseEvent = false;
        List<string>? releaseCandidates = null;
        var keyLock = this.EnterKeyLock( key );

        try
        {
            // An entry that the current instance has removed or replaced has already been handled. The callback may
            // also run after a newer value has been stored under the same key.
            if ( this._isDisposed || entryState.IsDetached )
            {
                return;
            }

            entryState.IsDetached = true;
            var currentItem = this.GetStoredItem( fullKey );

            if ( currentItem == null )
            {
                this._storedKeys.TryRemove( key, out _ );
                this.TrackUnknownDependencies( key, null );
            }

            if ( entryState.IsReplacement )
            {
                return;
            }

            if ( currentItem == null )
            {
                this.UnregisterRemovedItem( key, evictedItem, true, ref releaseCandidates );
                raiseEvent = true;
            }
            else if ( !evictedItem.Dependencies.IsDefaultOrEmpty )
            {
                // A newer value is stored. Only the dependencies that the newer value does not declare are unregistered.
                this.RemoveBackwardDependencies(
                    key,
                    evictedItem.Dependencies,
                    this.GetRegisteredDependencies( key, currentItem ),
                    ref releaseCandidates );
            }
        }
        finally
        {
            this.ExitKeyLock( key, keyLock );
        }

        this.ReleaseRegistrationsOfRemovedItems( releaseCandidates );

        if ( raiseEvent )
        {
            this.OnItemRemoved( key, CreateRemovalReason( reason ), this.Id );
        }
    }

    /// <summary>
    /// Unregisters an item that has been removed or evicted from the backward dependency sets of its forward dependencies.
    /// When <paramref name="retainForDependents"/> is <see langword="true"/> and an item that is in the cache depends on
    /// the key, the key stays registered and its forward dependencies are recorded in
    /// <see cref="_forwardDependenciesOfRemovedItems"/>, so that an invalidation of one of them still reaches the
    /// dependents. Must be called while the lock of the key is held.
    /// </summary>
    private void UnregisterRemovedItem( string key, MemoryCacheItem removedItem, bool retainForDependents, ref List<string>? releaseCandidates )
    {
        var registeredDependencies = this.GetRegisteredDependencies( key, removedItem );

        if ( registeredDependencies.IsEmpty )
        {
            return;
        }

        if ( retainForDependents && this.HasLiveDependents( key ) )
        {
            this._forwardDependenciesOfRemovedItems[key] = registeredDependencies;
        }
        else
        {
            this._forwardDependenciesOfRemovedItems.TryRemove( key, out _ );
            this.RemoveBackwardDependencies( key, registeredDependencies, ImmutableArray<string>.Empty, ref releaseCandidates );
        }
    }

    /// <inheritdoc />
    protected override void SetItemCore( string key, PSCacheItem item ) => this.SetItemImpl( key, item );

    /// <summary>
    /// Stores an item.
    /// </summary>
    /// <param name="key">The key of the item.</param>
    /// <param name="item">The item.</param>
    /// <param name="isMaskedBy">
    /// A predicate that receives the replacement value (tombstone) that is stored for the key, when there is one. When it
    /// returns <see langword="true"/>, the replacement value stays and the item is not stored. The predicate is called
    /// while the lock of the key is held, so it must not call the backend. <see langword="null"/> to store the item
    /// in any case.
    /// </param>
    /// <param name="isCopy">
    /// <see langword="true"/> when the item is a copy of an item of another layer, which a layered backend stores when
    /// it reads that layer. A copy does not replace a value in the sense of the cache, so the items that depend on the
    /// key are kept. When the dependencies of a copy are not known (<see cref="CacheItem.Dependencies"/> has its default
    /// value), the entry is removed by <see cref="RemoveItemsWithUnknownDependencies"/>.
    /// </param>
    /// <returns>
    /// The stored entry, or <see langword="null"/> when the item has not been stored, because a replacement value masks
    /// it or because the <see cref="IMemoryCache"/> did not keep it.
    /// </returns>
    /// <remarks>
    /// When the item replaces a value, the items that depend on the key are removed, directly and transitively, because
    /// they were computed from the replaced value.
    /// </remarks>
    internal MemoryCacheItem? SetItemImpl( string key, PSCacheItem item, Func<MemoryCacheItem, bool>? isMaskedBy = null, bool isCopy = false )
    {
        // The serializer and the size calculator are foreign code. They run before any lock is acquired and before any
        // state is changed, so that an exception leaves the backend unchanged and a callback that uses the backend cannot
        // deadlock with it.
        var entryState = new EntryState { HasUnknownDependencies = isCopy && item.Dependencies.IsDefault };
        var cacheValue = this.Serialize( new MemoryCacheItem( item.Value, item.Dependencies, entryState ) );
        var policy = this.CreatePolicy( item, cacheValue.Value );
        var newDependencies = cacheValue.Dependencies.IsDefaultOrEmpty ? ImmutableArray<string>.Empty : cacheValue.Dependencies;

        var itemKey = this.GetItemKey( key );
        List<string>? releaseCandidates = null;
        bool isStored;
        bool isPreviousValueLost;
        bool isValueReplaced;

        try
        {
            var keyLock = this.EnterKeyLock( key );

            try
            {
                var previousItem = this.GetStoredItem( itemKey );

                if ( isMaskedBy != null && previousItem is { State: EntryState { IsReplacement: true } } && isMaskedBy( previousItem ) )
                {
                    return null;
                }

                // The new dependencies are registered before the value is stored, so that an invalidation that follows the
                // store always finds the key.
                foreach ( var dependency in newDependencies )
                {
                    this.AddBackwardDependency( dependency, key );
                }

                // The previous entry is detached before it is replaced, so that its post-eviction callback does nothing.
                Detach( previousItem );
                this._storedKeys[key] = true;

                try
                {
                    this._cache.Set( itemKey, cacheValue, policy );
                }
                catch
                {
                    // The cache threw an exception, so the entry that it holds is unknown. The index is aligned with that
                    // entry before the exception reaches the caller.
                    this.AlignWithStoredItem( key, itemKey, cacheValue, previousItem, newDependencies, ref releaseCandidates, out _ );

                    throw;
                }

                isStored = this.AlignWithStoredItem(
                    key,
                    itemKey,
                    cacheValue,
                    previousItem,
                    newDependencies,
                    ref releaseCandidates,
                    out isPreviousValueLost );

                isValueReplaced = isStored && !isCopy && IsLive( previousItem );
            }
            finally
            {
                this.ExitKeyLock( key, keyLock );
            }
        }
        finally
        {
            this.ReleaseRegistrationsOfRemovedItems( releaseCandidates );
        }

        if ( isPreviousValueLost )
        {
            this.OnItemRemoved( key, CacheItemRemovedReason.Evicted, this.Id );
        }

        if ( isValueReplaced )
        {
            // The items that depend on the key were computed from the replaced value. The key itself is excluded, so that
            // a new value that depends on its own key, directly or through a dependency cycle, is kept.
            this.InvalidateDependents( key, null, null, new HashSet<string>( StringComparer.Ordinal ) { key }, key );
        }

        return isStored ? cacheValue : null;
    }

    /// <summary>
    /// Aligns the dependency index with the entry that the <see cref="IMemoryCache"/> holds for a key after an attempt
    /// to store a new entry. Must be called while the lock of the key is held.
    /// </summary>
    /// <param name="key">The key of the item.</param>
    /// <param name="itemKey">The key of the entry in the <see cref="IMemoryCache"/>.</param>
    /// <param name="newItem">The entry that the backend attempted to store.</param>
    /// <param name="previousItem">The entry that was stored before the attempt, or <see langword="null"/>.</param>
    /// <param name="newDependencies">The dependencies of <paramref name="newItem"/>, which are already registered.</param>
    /// <param name="releaseCandidates">The list that receives the keys of the removed items whose registrations may be released.</param>
    /// <param name="isPreviousValueLost">
    /// <see langword="true"/> when the cache removed the value of <paramref name="previousItem"/> without storing
    /// <paramref name="newItem"/>, so that the key went from a value to no value.
    /// </param>
    /// <returns><see langword="true"/> when the cache holds <paramref name="newItem"/>.</returns>
    private bool AlignWithStoredItem(
        string key,
        string itemKey,
        MemoryCacheItem newItem,
        MemoryCacheItem? previousItem,
        ImmutableArray<string> newDependencies,
        ref List<string>? releaseCandidates,
        out bool isPreviousValueLost )
    {
        isPreviousValueLost = false;

        var storedItem = this.GetStoredItem( itemKey );
        var previousDependencies = this.GetRegisteredDependencies( key, previousItem );

        this.TrackUnknownDependencies( key, storedItem );

        if ( ReferenceEquals( storedItem, newItem ) )
        {
            // The registrations of the previous item, and the registrations kept for a removed item, are replaced by the
            // registrations of the new item.
            if ( !previousDependencies.IsEmpty )
            {
                this._forwardDependenciesOfRemovedItems.TryRemove( key, out _ );
                this.RemoveBackwardDependencies( key, previousDependencies, newDependencies, ref releaseCandidates );
            }

            return true;
        }

        // The cache did not keep the new entry, for example because of its size limit. The post-eviction callback of
        // that entry does nothing.
        Detach( newItem );

        if ( storedItem != null )
        {
            // The cache holds another entry. This is the previous entry when the cache threw an exception before it
            // replaced that entry. The backend handles the later eviction of the previous entry.
            var storedDependencies = this.GetRegisteredDependencies( key, storedItem );

            if ( ReferenceEquals( storedItem, previousItem ) )
            {
                Attach( previousItem );
            }
            else
            {
                this.RemoveBackwardDependencies( key, previousDependencies, storedDependencies, ref releaseCandidates );
            }

            this.RemoveBackwardDependencies( key, newDependencies, storedDependencies, ref releaseCandidates );

            return false;
        }

        this._storedKeys.TryRemove( key, out _ );
        this.RemoveBackwardDependencies( key, newDependencies, previousDependencies, ref releaseCandidates );

        if ( IsLive( previousItem ) )
        {
            // A size-limited cache that rejects an entry also removes the previous entry of the key. The previous value
            // is handled as an evicted value.
            this.UnregisterRemovedItem( key, previousItem!, true, ref releaseCandidates );
            isPreviousValueLost = true;
        }

        return false;
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

    /// <summary>
    /// Removes the items that depend, directly or transitively, on a key, then raises the
    /// <see cref="CachingBackend.DependencyInvalidated"/> event.
    /// </summary>
    /// <param name="key">The invalidated dependency key.</param>
    /// <param name="replacementValue">
    /// A value that replaces each removed item for a limited time, or <see langword="null"/> to remove the items.
    /// </param>
    /// <param name="replacementValueExpiration">The instant, read from the clock of the backend, at which the replacement values expire.</param>
    internal void InvalidateDependencyImpl( string key, MemoryCacheItem? replacementValue = null, DateTimeOffset? replacementValueExpiration = null )
    {
        ValidateReplacement( replacementValue, replacementValueExpiration );

        this.InvalidateDependents( key, replacementValue, replacementValueExpiration, new HashSet<string>( StringComparer.Ordinal ), null );

        this.OnDependencyInvalidated( key, this.Id );
    }

    /// <summary>
    /// Removes the items registered under a dependency key, and recursively the items that depend on their keys.
    /// </summary>
    /// <remarks>
    /// <para>
    /// No lock is held across the recursion. A key is removed from the backward dependency set only after the items that depend
    /// on it have been processed, so that a concurrent invalidation of the same dependency still finds the key and walks
    /// its dependents before it returns.
    /// </para>
    /// <para>
    /// An item is removed only when its current value still declares the invalidated dependency. A value that a concurrent
    /// <see cref="CachingBackend.SetItem"/> has stored without that dependency is kept. The items that depend on a key
    /// whose item is absent are still processed, because their key may stay registered after the removal or the eviction
    /// of its item. The set of visited keys stops the recursion on a cyclic dependency graph.
    /// </para>
    /// </remarks>
    /// <param name="dependencyKey">The key whose dependents are removed.</param>
    /// <param name="replacementValue">A value that replaces each removed item for a limited time, or <see langword="null"/>.</param>
    /// <param name="replacementValueExpiration">The instant at which the replacement values expire.</param>
    /// <param name="visitedKeys">The keys whose dependents have been processed or are being processed.</param>
    /// <param name="excludedKey">
    /// A key whose item is never removed, or <see langword="null"/>. It is the key of an item that has just been stored,
    /// when the dependents of that key are removed because the item replaced a value.
    /// </param>
    private void InvalidateDependents(
        string dependencyKey,
        MemoryCacheItem? replacementValue,
        DateTimeOffset? replacementValueExpiration,
        HashSet<string> visitedKeys,
        string? excludedKey )
    {
        var dependents = this.GetDependents( dependencyKey );

        this.SyncPoint( SyncPoints.DependentsCopied );

        foreach ( var key in dependents )
        {
            if ( excludedKey != null && string.Equals( key, excludedKey, StringComparison.Ordinal ) )
            {
                continue;
            }

            var outcome = this.InvalidateItem( key, dependencyKey, replacementValue, replacementValueExpiration );

            if ( outcome.MustInvalidateDependents && visitedKeys.Add( key ) )
            {
                this.InvalidateDependents( key, replacementValue, replacementValueExpiration, visitedKeys, excludedKey );
            }

            if ( outcome.IsRemoved )
            {
                // The key of a removed item is itself an invalidated dependency, for the other layers and nodes that
                // subscribe to the events.
                this.OnDependencyInvalidated( key, this.Id );
                this.OnItemRemoved( key, CacheItemRemovedReason.Invalidated, this.Id );
            }

            this.UnregisterAfterInvalidation( key, dependencyKey );
        }
    }

    /// <summary>
    /// Removes the item of a key as a consequence of the invalidation of one of its dependencies.
    /// </summary>
    private (bool IsRemoved, bool MustInvalidateDependents) InvalidateItem(
        string key,
        string dependencyKey,
        MemoryCacheItem? replacementValue,
        DateTimeOffset? replacementValueExpiration )
    {
        var itemKey = this.GetItemKey( key );
        List<string>? releaseCandidates = null;
        (bool IsRemoved, bool MustInvalidateDependents) outcome;
        var keyLock = this.EnterKeyLock( key );

        try
        {
            this.SyncPoint( SyncPoints.ItemLocked );

            var currentItem = this.GetStoredItem( itemKey );

            if ( !IsLive( currentItem ) )
            {
                // The item is absent or already replaced. Its dependents may still be registered under its key. When
                // the item is absent, the replacement value is stored, so that it masks a value that another layer
                // still holds for the key. An existing replacement value is kept, because the copy of the backward
                // dependency set may be older than the removal that stored it.
                if ( currentItem == null && replacementValue != null )
                {
                    this.StoreReplacement( key, itemKey, replacementValue, replacementValueExpiration!.Value );
                }

                outcome = (false, true);
            }
            else if ( currentItem!.Dependencies.IsDefaultOrEmpty || !currentItem.Dependencies.Contains( dependencyKey ) )
            {
                // A concurrent SetItem has stored a value that does not depend on the invalidated key.
                outcome = (false, false);
            }
            else
            {
                this.RemoveCurrentItem( key, itemKey, currentItem, replacementValue, replacementValueExpiration, true, ref releaseCandidates );
                outcome = (true, true);
            }
        }
        finally
        {
            this.ExitKeyLock( key, keyLock );
        }

        this.ReleaseRegistrationsOfRemovedItems( releaseCandidates );

        return outcome;
    }

    /// <summary>
    /// Removes a key from the backward dependency set of an invalidated dependency after its dependents have been processed,
    /// unless the current value of the key still declares that dependency.
    /// </summary>
    private void UnregisterAfterInvalidation( string key, string dependencyKey )
    {
        List<string>? releaseCandidates = null;
        var keyLock = this.EnterKeyLock( key );

        try
        {
            var currentItem = this.GetStoredItem( this.GetItemKey( key ) );

            if ( IsLive( currentItem ) && !currentItem!.Dependencies.IsDefaultOrEmpty && currentItem.Dependencies.Contains( dependencyKey ) )
            {
                // The key has been stored again, with the same dependency, during the invalidation.
                return;
            }

            if ( this._forwardDependenciesOfRemovedItems.TryGetValue( key, out var forwardDependencies ) )
            {
                var remainingDependencies = forwardDependencies.Remove( dependencyKey, StringComparer.Ordinal );

                if ( remainingDependencies.IsEmpty )
                {
                    this._forwardDependenciesOfRemovedItems.TryRemove( key, out _ );
                }
                else
                {
                    this._forwardDependenciesOfRemovedItems[key] = remainingDependencies;
                }
            }

            this.RemoveBackwardDependency( dependencyKey, key, ref releaseCandidates );
        }
        finally
        {
            this.ExitKeyLock( key, keyLock );
        }

        this.ReleaseRegistrationsOfRemovedItems( releaseCandidates );
    }

    /// <summary>
    /// Removes the item of a key.
    /// </summary>
    /// <param name="key">The key of the item.</param>
    /// <param name="replacementValue">
    /// A value that replaces the item for a limited time, or <see langword="null"/> to remove the item. The replacement
    /// value is stored even when the key has no item, so that it masks a value that another layer still holds.
    /// </param>
    /// <param name="replacementValueExpiration">The instant, read from the clock of the backend, at which the replacement value expires.</param>
    /// <param name="expectedItem">
    /// An entry returned by <see cref="SetItemImpl"/>, to do nothing unless the key still holds that entry, or
    /// <see langword="null"/> to remove any entry.
    /// </param>
    /// <returns><see langword="true"/> when a value has been removed or replaced.</returns>
    internal bool RemoveItemImpl(
        string key,
        MemoryCacheItem? replacementValue = null,
        DateTimeOffset? replacementValueExpiration = null,
        MemoryCacheItem? expectedItem = null )
    {
        ValidateReplacement( replacementValue, replacementValueExpiration );

        var itemKey = this.GetItemKey( key );
        List<string>? releaseCandidates = null;
        bool isRemoved;
        var keyLock = this.EnterKeyLock( key );

        try
        {
            this.SyncPoint( SyncPoints.ItemLocked );

            var currentItem = this.GetStoredItem( itemKey );

            if ( expectedItem != null && !ReferenceEquals( currentItem, expectedItem ) )
            {
                return false;
            }

            isRemoved = IsLive( currentItem );

            this.RemoveCurrentItem( key, itemKey, currentItem, replacementValue, replacementValueExpiration, true, ref releaseCandidates );
        }
        finally
        {
            this.ExitKeyLock( key, keyLock );
        }

        this.ReleaseRegistrationsOfRemovedItems( releaseCandidates );

        return isRemoved;
    }

    /// <summary>
    /// Removes or replaces the current entry of a key and unregisters its dependencies. Must be called while the lock of
    /// the key is held.
    /// </summary>
    private void RemoveCurrentItem(
        string key,
        string itemKey,
        MemoryCacheItem? currentItem,
        MemoryCacheItem? replacementValue,
        DateTimeOffset? replacementValueExpiration,
        bool retainForDependents,
        ref List<string>? releaseCandidates )
    {
        Detach( currentItem );

        if ( replacementValue != null )
        {
            this.StoreReplacement( key, itemKey, replacementValue, replacementValueExpiration!.Value );
        }
        else if ( currentItem != null )
        {
            this._cache.Remove( itemKey );
            this._storedKeys.TryRemove( key, out _ );
        }

        // Neither a removed entry nor a replacement value is a copy with unknown dependencies.
        this.TrackUnknownDependencies( key, null );

        if ( IsLive( currentItem ) )
        {
            this.UnregisterRemovedItem( key, currentItem!, retainForDependents, ref releaseCandidates );
        }
    }

    /// <summary>
    /// Records whether the entry stored for a key is a copy with unknown dependencies. Must be called while the lock of
    /// the key is held.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="storedItem">The entry that is stored for the key, or <see langword="null"/>.</param>
    private void TrackUnknownDependencies( string key, MemoryCacheItem? storedItem )
    {
        if ( storedItem?.State is EntryState { HasUnknownDependencies: true } )
        {
            this._keysWithUnknownDependencies[key] = true;
        }
        else if ( this._keysWithUnknownDependencies.ContainsKey( key ) )
        {
            this._keysWithUnknownDependencies.TryRemove( key, out _ );
        }
    }

    /// <summary>
    /// Removes every entry that is a copy of an item of another layer and whose dependencies are unknown.
    /// </summary>
    /// <remarks>
    /// A layered backend calls this method when a dependency is invalidated, because the dependency index cannot tell
    /// which of these entries depend on the invalidated dependency. The removed entries are read again from the other
    /// layer when they are needed. No event is raised, because the items are still valid in the other layer unless that
    /// layer removes them.
    /// </remarks>
    internal void RemoveItemsWithUnknownDependencies()
    {
        foreach ( var pair in this._keysWithUnknownDependencies )
        {
            this.RemoveItemWithUnknownDependencies( pair.Key );
        }
    }

    /// <summary>
    /// Removes the entry of a key when it is a copy of an item of another layer and its dependencies are unknown.
    /// </summary>
    /// <param name="key">The key of the item.</param>
    /// <returns><see langword="true"/> when the entry has been removed.</returns>
    internal bool RemoveItemWithUnknownDependencies( string key )
    {
        if ( !this._keysWithUnknownDependencies.ContainsKey( key ) )
        {
            return false;
        }

        var itemKey = this.GetItemKey( key );
        List<string>? releaseCandidates = null;
        bool isRemoved;
        var keyLock = this.EnterKeyLock( key );

        try
        {
            var currentItem = this.GetStoredItem( itemKey );
            isRemoved = currentItem?.State is EntryState { HasUnknownDependencies: true };

            if ( isRemoved )
            {
                this.RemoveCurrentItem( key, itemKey, currentItem, null, null, true, ref releaseCandidates );
            }
            else
            {
                this.TrackUnknownDependencies( key, currentItem );
            }
        }
        finally
        {
            this.ExitKeyLock( key, keyLock );
        }

        this.ReleaseRegistrationsOfRemovedItems( releaseCandidates );

        return isRemoved;
    }

    /// <summary>
    /// Stores the replacement value (tombstone) of a removed item. Must be called while the lock of the key is held.
    /// </summary>
    /// <remarks>
    /// The expiration is converted to a duration with the clock of the backend, because the <see cref="IMemoryCache"/>
    /// evaluates it with its own clock. The entry is never evicted for capacity, so that it keeps masking the value of
    /// the other layer during its lifetime, and it has a size, as a size-limited <see cref="MemoryCache"/> requires.
    /// </remarks>
    private void StoreReplacement( string key, string itemKey, MemoryCacheItem replacementValue, DateTimeOffset expiration )
    {
        var lifetime = expiration - this.TimeProvider.GetUtcNow();

        if ( lifetime <= TimeSpan.Zero )
        {
            lifetime = TimeSpan.FromTicks( 1 );
        }

        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = lifetime, Priority = CacheItemPriority.NeverRemove, Size = 0 };
        options.RegisterPostEvictionCallback( this.OnCacheItemRemoved );

        this._storedKeys[key] = true;
        this._cache.Set( itemKey, replacementValue with { State = new EntryState { IsReplacement = true } }, options );
    }

    private static void ValidateReplacement( MemoryCacheItem? replacementValue, DateTimeOffset? replacementValueExpiration )
    {
        if ( replacementValue != null && replacementValueExpiration == null )
        {
            throw new ArgumentException(
                "If " + nameof(replacementValue) + " is specified, " + nameof(replacementValueExpiration) + " must also be specified." );
        }
    }

    /// <inheritdoc />
    protected override bool ContainsDependencyCore( string key ) => this.HasDependents( key );

    /// <param name="options"></param>
    /// <inheritdoc />
    /// <remarks>
    /// <para>
    /// With <see cref="ClearCacheOptions.Compact"/>, when the current instance owns its <see cref="IMemoryCache"/>, the
    /// cache is compacted, and the post-eviction callbacks unregister the evicted items and raise the
    /// <see cref="CachingBackend.ItemRemoved"/> event.
    /// </para>
    /// <para>
    /// Otherwise, only the items of the current instance are removed, one key at a time under the lock of the key, so that
    /// other users of a shared <see cref="IMemoryCache"/> are not affected and a concurrent
    /// <see cref="CachingBackend.SetItem"/> leaves a consistent state. With <see cref="ClearCacheOptions.Compact"/>, the
    /// <see cref="CachingBackend.ItemRemoved"/> event is raised for each removed item.
    /// </para>
    /// </remarks>
    protected override void ClearCore( ClearCacheOptions options )
    {
        this.SyncPoint( SyncPoints.ClearRequested );

        var raiseEvents = (options & ClearCacheOptions.Compact) != 0;

        if ( raiseEvents && this._ownsCache )
        {
            switch ( this._cache )
            {
                case MemoryCache memoryCache:
                    memoryCache.Compact( 1 );

                    return;

                case IClearableMemoryCache clearableMemoryCache:
                    clearableMemoryCache.Compact( 1 );

                    return;
            }
        }

        foreach ( var key in this._storedKeys.Keys.Concat( this._forwardDependenciesOfRemovedItems.Keys ).Distinct( StringComparer.Ordinal ).ToList() )
        {
            var itemKey = this.GetItemKey( key );
            List<string>? releaseCandidates = null;
            bool isRemoved;
            var keyLock = this.EnterKeyLock( key );

            try
            {
                var currentItem = this.GetStoredItem( itemKey );
                isRemoved = IsLive( currentItem );

                Detach( currentItem );

                if ( currentItem != null )
                {
                    this._cache.Remove( itemKey );
                }

                this._storedKeys.TryRemove( key, out _ );
                this.TrackUnknownDependencies( key, null );

                var registeredDependencies = this.GetRegisteredDependencies( key, currentItem );
                this._forwardDependenciesOfRemovedItems.TryRemove( key, out _ );
                this.RemoveBackwardDependencies( key, registeredDependencies, ImmutableArray<string>.Empty, ref releaseCandidates );
            }
            finally
            {
                this.ExitKeyLock( key, keyLock );
            }

            this.ReleaseRegistrationsOfRemovedItems( releaseCandidates );

            if ( isRemoved && raiseEvents )
            {
                this.OnItemRemoved( key, CacheItemRemovedReason.Evicted, this.Id );
            }
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
        return new Features();
    }

    /// <inheritdoc />
    protected override void DisposeCore( bool disposing, CancellationToken cancellationToken )
    {
        base.DisposeCore( disposing, cancellationToken );
        this.ReleaseCache();
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore( CancellationToken cancellationToken )
    {
        await base.DisposeAsyncCore( cancellationToken ).ConfigureAwait( false );
        this.ReleaseCache();
    }

    /// <summary>
    /// Disposes the <see cref="IMemoryCache"/> when the current instance owns it. Otherwise, removes the entries of the
    /// current instance and leaves the <see cref="IMemoryCache"/> usable by its other users.
    /// </summary>
    private void ReleaseCache()
    {
        // The post-eviction callbacks that run from now on do nothing.
        this._isDisposed = true;

        if ( this._ownsCache )
        {
            this._cache.Dispose();
        }
        else
        {
            foreach ( var key in this._storedKeys.Keys.ToList() )
            {
                this._cache.Remove( this.GetItemKey( key ) );
            }
        }

        this._storedKeys.Clear();
        this._keysWithUnknownDependencies.Clear();
        this._backwardDependencies.Clear();
        this._forwardDependenciesOfRemovedItems.Clear();
    }
}
