// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Engine.CodeModel.GenericContexts;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.CodeModel.Factories;

public sealed partial class DeclarationFactory
{
    private class Cache<TKey, TValue>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, ConcurrentDictionary<ItemKey, TValue>> _items;

        public Cache( IEqualityComparer<TKey> comparer )
        {
            this._items = new ConcurrentDictionary<TKey, ConcurrentDictionary<ItemKey, TValue>>( comparer );
        }

        public TValue GetOrAdd<TArg>(
            TKey item,
            GenericContext? genericContext,
            Type requiredInterface,
            Func<TKey, GenericContext?, TArg, TValue> factory,
            TArg arg )
        {
            var bucket = this._items.GetOrAdd( item, _ => new ConcurrentDictionary<ItemKey, TValue>() );
            var key = new ItemKey( genericContext ?? GenericContext.Empty, requiredInterface );

            if ( bucket.TryGetValue( key, out var value ) )
            {
                return value;
            }
            else
            {
                value = factory( item, genericContext, arg );

                return bucket.GetOrAdd( key, value );
            }
        }

        public void Remove( TKey item )
        {
            this._items.TryRemove( item, out _ );
        }

        private readonly struct ItemKey : IEquatable<ItemKey>
        {
            private readonly GenericContext _genericContext;

            private readonly Type _interface;

            public ItemKey( GenericContext genericContext, Type @interface )
            {
                this._genericContext = genericContext;
                this._interface = @interface;
            }

            public bool Equals( ItemKey other )
            {
                if ( this._interface != other._interface )
                {
                    return false;
                }

                if ( !this._genericContext.Equals( other._genericContext ) )
                {
                    return false;
                }

                return true;
            }

            public override bool Equals( object? obj ) => obj is ItemKey other && this.Equals( other );

            public override int GetHashCode() => HashCode.Combine( this._genericContext, this._interface );
        }
    }
}