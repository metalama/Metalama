// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Metalama.Framework.Engine.Utilities.Caching;

/// <summary>
/// A base cache with key-level locking based on a rotation strategy instead of a cache sweeping strategy
/// that would require the enumeration of all items in the cache.
/// </summary>
public abstract class Cache<TKey, TValue, TTag> : ICache<TKey, TValue>
    where TKey : notnull
{
    private readonly IEqualityComparer<TKey> _keyComparer;
    private readonly ConcurrentDictionary<TKey, object> _locks;
    private readonly ThreadLocal<bool> _holdsLock = new();
    private volatile Caches _caches;
    private volatile int _rotating;

    protected Cache( IEqualityComparer<TKey>? keyComparer = null )
    {
        this._keyComparer = keyComparer ?? EqualityComparer<TKey>.Default;
        this._caches = new Caches( this.CreateConcurrentDictionary(), null );
        this._locks = new ConcurrentDictionary<TKey, object>( this._keyComparer );
    }

    private ConcurrentDictionary<TKey, Item> CreateConcurrentDictionary( int capacity = 0 )
        => new( ConcurrencyLevel, Math.Max( capacity, 64 ), this._keyComparer );

    protected int RecentItemsCount => this._caches.Recent.Count;

    internal int Count
    {
        get
        {
            var caches = this._caches;

            return caches.Recent.Count + caches.Old?.Count ?? 0;
        }
    }

    private static int ConcurrencyLevel => Environment.ProcessorCount;

    public bool TryGetValue( TKey key, out TValue value )
    {
        var caches = this._caches;

        if ( !caches.Recent.TryGetValue( key, out var item ) )
        {
            if ( caches.Old != null && caches.Old.TryGetValue( key, out item ) )
            {
                if ( !this.Validate( key, item ) )
                {
                    caches.Old.TryRemove( key, out _ );

                    value = default!;

                    return false;
                }
                else
                {
                    if ( caches.Recent.TryAdd( key, item ) )
                    {
                        caches.Old.TryRemove( key, out _ );
                    }

                    value = item.Value;

                    return true;
                }
            }
            else
            {
                value = default!;

                return false;
            }
        }
        else
        {
            if ( !this.Validate( key, item ) )
            {
                caches.Recent.TryRemove( key, out _ );

                value = default!;

                return false;
            }
            else
            {
                value = item.Value;

                return true;
            }
        }
    }

    protected virtual bool Validate( TKey key, in Item item ) => true;

    protected abstract bool ShouldRotate();

    protected virtual void OnRotated() { }

    protected virtual TTag GetTag( TKey key ) => default!;

    public TValue GetOrAdd( TKey key, Func<TKey, TValue> func )
    {
        // Find an existing item.
        if ( this.TryGetValue( key, out var value ) )
        {
            return value;
        }

        // Rotate the cache if necessary.
        this.Rotate();

        if ( this._holdsLock.Value )
        {
            value = func( key );

            if ( this.TryGetValue( key, out var recursiveValue ) )
            {
                return recursiveValue;
            }
            else
            {
                var item = new Item( value, this.GetTag( key ) );

                this._caches.Recent[key] = item;

                return value;
            }
        }
        else
        {
            // There may a race of several threads wanting to add the item to the cache.
            // We solve this problem by having a dictionary of locks. Each thread tries to have its own monitor to the dictionary.
            // The thread that wins adds the item to the cache. The other threads have to wait.

            // Create our own monitor and acquires it. We have to acquire it _before_ adding it to the dictionary of locks.
            var ourMonitor = new object();

            lock ( ourMonitor )
            {
                while ( true )
                {
                    var sharedMonitor = this._locks.GetOrAdd( key, ourMonitor );

                    if ( sharedMonitor == ourMonitor )
                    {
                        // We won the race.
                        try
                        {
                            this._holdsLock.Value = true;

                            // Create the new item.
                            value = func( key );
                            var item = new Item( value, this.GetTag( key ) );

                            if ( this._caches.Recent.TryGetValue( key, out var recursiveItem ) )
                            {
                                return recursiveItem.Value;
                            }
                            else
                            {
                                this._caches.Recent[key] = item;
                            }

                            return value;
                        }
                        finally
                        {
                            this._holdsLock.Value = false;
                            this._locks.TryRemove( key, out _ );
                        }
                    }
                    else
                    {
                        // We lost the race, so we have to wait.
                        Monitor.Enter( sharedMonitor );
                        Monitor.Exit( sharedMonitor );

                        if ( this.TryGetValue( key, out value ) )
                        {
                            return value;
                        }
                    }
                }
            }
        }
    }

    public bool TryAdd( TKey key, TValue value )
    {
        // Find an existing item.
        if ( this.TryGetValue( key, out _ ) )
        {
            return false;
        }

        // Rotate the cache if necessary.
        this.Rotate();

        // There may a race of several threads wanting to add the item to the cache.
        // We solve this problem by having a dictionary of locks. Each thread tries to have its own monitor to the dictionary.
        // The thread that wins adds the item to the cache. The other threads have to wait.

        // Create our own monitor and acquires it. We have to acquire it _before_ adding it to the dictionary of locks.
        var ourMonitor = new object();

        lock ( ourMonitor )
        {
            while ( true )
            {
                var sharedMonitor = this._locks.GetOrAdd( key, ourMonitor );

                if ( sharedMonitor == ourMonitor )
                {
                    // We won the race.
                    try
                    {
                        // Create the new item.
                        var item = new Item( value, this.GetTag( key ) );

                        // We replace the cache item, so in case there is a concurrent
                        this._caches.Recent[key] = item;

                        return true;
                    }
                    finally
                    {
                        this._locks.TryRemove( key, out _ );
                    }
                }
                else
                {
                    // We lost the race, so we have to wait.
                    Monitor.Enter( sharedMonitor );
                    Monitor.Exit( sharedMonitor );

                    return false;
                }
            }
        }
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public bool TryRemove( TKey key )
    {
        var caches = this._caches;

        if ( caches.Recent.TryRemove( key, out _ ) )
        {
            return true;
        }

        if ( caches.Old?.TryRemove( key, out _ ) == true )
        {
            return true;
        }

        return false;
    }

    private void Rotate()
    {
        if ( this.ShouldRotate() )
        {
            if ( Interlocked.CompareExchange( ref this._rotating, 1, 0 ) == 0 )
            {
                if ( this.ShouldRotate() )
                {
                    var previousCaches = this._caches;

                    // We clear the cache of old items even if this it can be accessed at the moment by a getter thread.
                    // During a very short time, this may result in cache misses that could be avoided by allocated a new dictionary instead
                    // of reusing the previous one. However, we assume that we get better performance by reusing the existing dictionary.
                    previousCaches.Old?.Clear();

                    // ReSharper disable once UseWithExpressionToCopyRecord
                    this._caches = new Caches( previousCaches.Old ?? this.CreateConcurrentDictionary( previousCaches.Recent.Count ), previousCaches.Recent );
                }

                this.OnRotated();

                this._rotating = 0;
            }
        }
    }

    private sealed record Caches( ConcurrentDictionary<TKey, Item> Recent, ConcurrentDictionary<TKey, Item>? Old );

    protected record struct Item( TValue Value, TTag Tag );

    public void Dispose() => this._holdsLock.Dispose();
}