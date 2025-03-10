// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System;
using System.Runtime.CompilerServices;

namespace Metalama.Framework.Engine.Utilities.Caching;

/// <summary>
/// A cache based on <see cref="ConditionalWeakTable{TKey,TValue}"/>, which holds a weak reference to the key.
/// </summary>
public sealed class WeakCache<TKey, TValue> : ICache<TKey, TValue>
    where TKey : class
{
    private readonly ConditionalWeakTable<TKey, StrongBox<TValue>> _cache = new();

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