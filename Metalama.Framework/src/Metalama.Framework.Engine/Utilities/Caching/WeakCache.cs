// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Engine.Utilities.Caching;

/// <summary>
/// Abstract base class for weak caches that supports invalidation of static instances.
/// </summary>
public abstract class WeakCache
{
    private static readonly object _lock = new();
    private static readonly List<WeakReference<WeakCache>> _staticCaches = new();

    /// <summary>
    /// Invalidates all registered static caches by calling <see cref="Clear"/> on each.
    /// </summary>
    public static void Invalidate()
    {
        lock ( _lock )
        {
            for ( var i = _staticCaches.Count - 1; i >= 0; i-- )
            {
                if ( _staticCaches[i].TryGetTarget( out var cache ) )
                {
                    cache.Clear();
                }
                else
                {
                    // Remove dead references
                    _staticCaches.RemoveAt( i );
                }
            }
        }
    }

    /// <summary>
    /// Registers this cache instance as a static cache that will be cleared when <see cref="Invalidate"/> is called.
    /// </summary>
    private protected void Register()
    {
        lock ( _lock )
        {
            _staticCaches.Add( new WeakReference<WeakCache>( this ) );
        }
    }

    /// <summary>
    /// Clears all entries in the cache.
    /// </summary>
    public abstract void Clear();
}

/// <summary>
/// A cache based on <see cref="ConditionalWeakTable{TKey,TValue}"/>, which holds a weak reference to the key.
/// </summary>
public sealed class WeakCache<TKey, TValue> : WeakCache, ICache<TKey, TValue>
    where TKey : class
{
    private ConditionalWeakTable<TKey, StrongBox<TValue>> _cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="WeakCache{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="isStaticCache">If <c>true</c>, the cache will be registered for invalidation via <see cref="WeakCache.Invalidate"/>.</param>
    public WeakCache( bool isStaticCache = false )
    {
        if ( isStaticCache )
        {
            this.Register();
        }
    }

    /// <inheritdoc />
    public override void Clear()
    {
        this._cache = new ConditionalWeakTable<TKey, StrongBox<TValue>>();
    }

    public bool TryGetValue( TKey key, out TValue value )
    {
        // ReSharper disable once InconsistentlySynchronizedField
        if ( this._cache.TryGetValue( key, out var box ) )
        {
            value = box.AssertNotNull().Value!;

            return true;
        }
        else
        {
            value = default!;

            return false;
        }
    }

    public TValue GetOrAdd( TKey key, Func<TKey, TValue> func )
    {
        if ( this.TryGetValue( key, out var value ) )
        {
            return value;
        }

        lock ( key )
        {
            while ( true )
            {
                // We won the race.
                // Create the new item.
                value = func( key );

                // The func may have added the same item to the cache.
                if ( this.TryGetValue( key, out var recursiveValue ) )
                {
                    return recursiveValue;
                }

                this._cache.Add( key, new StrongBox<TValue>( value ) );

                return value;
            }
        }
    }

    internal TValue GetOrAdd<TPayload>( TKey key, Func<TKey, TPayload, TValue> func, TPayload payload )
    {
        if ( this.TryGetValue( key, out var value ) )
        {
            return value;
        }

        lock ( key )
        {
            while ( true )
            {
                // We won the race.
                // Create the new item.
                value = func( key, payload );

                // The func may have added the same item to the cache.
                if ( this.TryGetValue( key, out var recursiveValue ) )
                {
                    return recursiveValue;
                }

                this._cache.Add( key, new StrongBox<TValue>( value ) );

                return value;
            }
        }
    }

    public bool TryAdd( TKey key, TValue value )
    {
        if ( this.TryGetValue( key, out _ ) )
        {
            return false;
        }

        lock ( key )
        {
            if ( this.TryGetValue( key, out _ ) )
            {
                return false;
            }

            this._cache.Add( key, new StrongBox<TValue>( value ) );

            return true;
        }
    }

    public void Dispose() { }
}
