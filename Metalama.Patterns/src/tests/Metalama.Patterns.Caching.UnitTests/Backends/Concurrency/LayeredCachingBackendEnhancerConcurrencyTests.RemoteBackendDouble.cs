// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using System.Collections.Concurrent;

namespace Metalama.Patterns.Caching.Tests.Backends.Concurrency
{
    public sealed partial class LayeredCachingBackendEnhancerConcurrencyTests
    {
        /// <summary>
        /// A second-layer backend that stores the <see cref="CacheItem"/> objects that it receives. A test controls when a
        /// removal takes effect, and which source identifier the events carry.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The double behaves like the Redis dependency backend in the respects on which
        /// <see cref="LayeredCachingBackendEnhancer"/> depends. It returns the <see cref="MaterializedCacheItem"/> that
        /// the enhancer stored, which the enhancer casts. When the caller does not request the dependencies, it returns a
        /// copy whose <see cref="CacheItem.Dependencies"/> property has the default value. It raises its events with its
        /// own <see cref="CachingBackend.Id"/>, unless the constructor receives another source identifier.
        /// </para>
        /// <para>
        /// A test passes the double directly to <see cref="LayeredCachingBackendEnhancer"/>, as the layered builder
        /// passes the remote backend. An enhancer between the two would forward the source identifier of the inner
        /// backend. That identifier differs from the identifier of the enhancer, so the layered enhancer would process
        /// the events as events of another node.
        /// </para>
        /// <para>
        /// When the double defers its removals, <see cref="CachingBackend.RemoveItem"/> returns before the item is
        /// removed, as the removal of a backend that is not blocking does. The removal takes effect when the test calls
        /// <see cref="CompletePendingRemovals"/>. An invalidation of a dependency always takes effect immediately.
        /// </para>
        /// </remarks>
        private sealed partial class RemoteBackendDouble : CachingBackend
        {
            /// <summary>
            /// The object that protects the items, the dependencies and the pending removals.
            /// </summary>
            private readonly object _sync = new();

            /// <summary>
            /// The stored items, by key.
            /// </summary>
            private readonly Dictionary<string, CacheItem> _items = new( StringComparer.Ordinal );

            /// <summary>
            /// The keys of the items that depend on each dependency, by dependency.
            /// </summary>
            private readonly Dictionary<string, HashSet<string>> _dependencies = new( StringComparer.Ordinal );

            /// <summary>
            /// The keys whose removal has been requested and has not taken effect yet.
            /// </summary>
            private readonly List<string> _pendingRemovals = new();

            /// <summary>
            /// A description of each read, in the order of the reads.
            /// </summary>
            private readonly ConcurrentQueue<string> _reads = new();

            /// <summary>
            /// The value of <see cref="CachingBackendFeatures.Blocking"/>.
            /// </summary>
            private readonly bool _blocking;

            /// <summary>
            /// A value indicating whether the double raises events.
            /// </summary>
            private readonly bool _raisesEvents;

            /// <summary>
            /// A value indicating whether a removal takes effect only when the test calls <see cref="CompletePendingRemovals"/>.
            /// </summary>
            private readonly bool _defersRemovals;

            /// <summary>
            /// The source identifier of the events, or <see langword="null"/> to use <see cref="CachingBackend.Id"/>.
            /// </summary>
            private readonly Guid? _eventSourceId;

            /// <summary>
            /// Initializes a new instance of the <see cref="RemoteBackendDouble"/> class.
            /// </summary>
            /// <param name="serviceProvider">
            /// The service provider, which supplies the clock and the work-item dispatcher of the events, or
            /// <see langword="null"/> for the system clock and the thread pool.
            /// </param>
            /// <param name="blocking">The value of <see cref="CachingBackendFeatures.Blocking"/>.</param>
            /// <param name="raisesEvents">
            /// <see langword="true"/> to report the <see cref="CachingBackendFeatures.Events"/> feature and raise the
            /// <see cref="CachingBackend.ItemRemoved"/> and <see cref="CachingBackend.DependencyInvalidated"/> events.
            /// </param>
            /// <param name="defersRemovals">
            /// <see langword="true"/> to make a removal take effect only when the test calls
            /// <see cref="CompletePendingRemovals"/>.
            /// </param>
            /// <param name="eventSourceId">
            /// The source identifier of the events, or <see langword="null"/> to use <see cref="CachingBackend.Id"/>.
            /// </param>
            public RemoteBackendDouble(
                IServiceProvider? serviceProvider,
                bool blocking = true,
                bool raisesEvents = false,
                bool defersRemovals = false,
                Guid? eventSourceId = null )
                : base( serviceProvider: serviceProvider )
            {
                this._blocking = blocking;
                this._raisesEvents = raisesEvents;
                this._defersRemovals = defersRemovals;
                this._eventSourceId = eventSourceId;
            }

            /// <summary>
            /// Gets a description of each read, in the order of the reads.
            /// </summary>
            public IReadOnlyList<string> Reads => this._reads.ToArray();

            /// <summary>
            /// Gets the number of removals that have been requested and have not taken effect yet.
            /// </summary>
            public int PendingRemovalCount
            {
                get
                {
                    lock ( this._sync )
                    {
                        return this._pendingRemovals.Count;
                    }
                }
            }

            /// <summary>
            /// Gets the source identifier of the events.
            /// </summary>
            private Guid EventSourceId => this._eventSourceId ?? this.Id;

            /// <summary>
            /// Applies the removals that have been requested and have not taken effect yet, on the calling thread.
            /// </summary>
            public void CompletePendingRemovals()
            {
                List<string> removals;

                lock ( this._sync )
                {
                    removals = this._pendingRemovals.ToList();
                    this._pendingRemovals.Clear();
                }

                foreach ( var key in removals )
                {
                    this.RemoveNow( key );
                }
            }

            /// <inheritdoc />
            protected override CachingBackendFeatures CreateFeatures() => new DoubleFeatures( this._blocking, this._raisesEvents );

            /// <inheritdoc />
            protected override void SetItemCore( string key, CacheItem item )
            {
                lock ( this._sync )
                {
                    if ( this._items.TryGetValue( key, out var previousItem ) )
                    {
                        this.UnregisterNoLock( key, previousItem );
                    }

                    this._items[key] = item;

                    if ( !item.Dependencies.IsDefaultOrEmpty )
                    {
                        foreach ( var dependency in item.Dependencies )
                        {
                            if ( !this._dependencies.TryGetValue( dependency, out var dependentKeys ) )
                            {
                                dependentKeys = new HashSet<string>( StringComparer.Ordinal );
                                this._dependencies[dependency] = dependentKeys;
                            }

                            dependentKeys.Add( key );
                        }
                    }
                }
            }

            /// <inheritdoc />
            protected override CacheItem? GetItemCore( string key, bool includeDependencies )
            {
                CacheItem? storedItem;

                lock ( this._sync )
                {
                    this._items.TryGetValue( key, out storedItem );
                }

                CacheItem? result;

                if ( storedItem == null || includeDependencies )
                {
                    result = storedItem;
                }
                else
                {
                    // The Redis dependency backend reads the dependencies only when the caller requests them.
                    result = storedItem with { Dependencies = default };
                }

                this._reads.Enqueue( $"GetItem( \"{key}\", includeDependencies: {includeDependencies} ) returned {DescribeItem( result )}" );

                return result;
            }

            /// <inheritdoc />
            protected override bool ContainsItemCore( string key )
            {
                lock ( this._sync )
                {
                    return this._items.ContainsKey( key );
                }
            }

            /// <inheritdoc />
            protected override void RemoveItemCore( string key )
            {
                if ( this._defersRemovals )
                {
                    lock ( this._sync )
                    {
                        this._pendingRemovals.Add( key );
                    }
                }
                else
                {
                    this.RemoveNow( key );
                }
            }

            /// <inheritdoc />
            protected override void InvalidateDependencyCore( string key )
            {
                var removedKeys = new List<string>();

                lock ( this._sync )
                {
                    if ( this._dependencies.TryGetValue( key, out var dependentKeys ) )
                    {
                        this._dependencies.Remove( key );

                        foreach ( var dependentKey in dependentKeys.ToList() )
                        {
                            if ( this._items.TryGetValue( dependentKey, out var dependentItem ) )
                            {
                                this._items.Remove( dependentKey );
                                this.UnregisterNoLock( dependentKey, dependentItem );
                                removedKeys.Add( dependentKey );
                            }
                        }
                    }
                }

                if ( this._raisesEvents )
                {
                    foreach ( var removedKey in removedKeys )
                    {
                        this.OnItemRemoved( removedKey, CacheItemRemovedReason.Invalidated, this.EventSourceId );
                    }

                    this.OnDependencyInvalidated( key, this.EventSourceId );
                }
            }

            /// <inheritdoc />
            protected override bool ContainsDependencyCore( string key )
            {
                lock ( this._sync )
                {
                    return this._dependencies.ContainsKey( key );
                }
            }

            /// <inheritdoc />
            protected override void ClearCore( ClearCacheOptions options )
            {
                lock ( this._sync )
                {
                    this._items.Clear();
                    this._dependencies.Clear();
                    this._pendingRemovals.Clear();
                }
            }

            /// <summary>
            /// Removes the item of <paramref name="key"/> and raises the <see cref="CachingBackend.ItemRemoved"/> event
            /// when the double raises events.
            /// </summary>
            /// <param name="key">The key of the item.</param>
            private void RemoveNow( string key )
            {
                var removed = false;

                lock ( this._sync )
                {
                    if ( this._items.TryGetValue( key, out var item ) )
                    {
                        this._items.Remove( key );
                        this.UnregisterNoLock( key, item );
                        removed = true;
                    }
                }

                if ( removed && this._raisesEvents )
                {
                    this.OnItemRemoved( key, CacheItemRemovedReason.Removed, this.EventSourceId );
                }
            }

            /// <summary>
            /// Removes <paramref name="key"/> from the dependency sets of <paramref name="item"/>. The caller holds
            /// <see cref="_sync"/>.
            /// </summary>
            /// <param name="key">The key of the item.</param>
            /// <param name="item">The item whose dependencies are unregistered.</param>
            private void UnregisterNoLock( string key, CacheItem item )
            {
                if ( item.Dependencies.IsDefaultOrEmpty )
                {
                    return;
                }

                foreach ( var dependency in item.Dependencies )
                {
                    if ( this._dependencies.TryGetValue( dependency, out var dependentKeys ) )
                    {
                        dependentKeys.Remove( key );

                        if ( dependentKeys.Count == 0 )
                        {
                            this._dependencies.Remove( dependency );
                        }
                    }
                }
            }
        }
    }
}