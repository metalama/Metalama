// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Patterns.Caching.Backends;
using Metalama.Patterns.Caching.Implementation;
using System.Collections.Concurrent;

namespace Metalama.Patterns.Caching.Tests.Backends;

public sealed partial class LayeredCachingBackendEnhancerTests
{
    /// <summary>
    /// A simple test backend that stores <see cref="CacheItem"/> objects directly in memory,
    /// preserving their actual runtime type (including <see cref="MaterializedCacheItem"/>).
    /// This allows testing L2 fallback behavior in <see cref="LayeredCachingBackendEnhancer"/>.
    /// </summary>
    private sealed class TypePreservingBackend : CachingBackend
    {
        private readonly ConcurrentDictionary<string, CacheItem> _items = new();
        private readonly ConcurrentDictionary<string, HashSet<string>> _dependencies = new();
        private readonly bool _blocking;

        public TypePreservingBackend( bool blocking = true )
        {
            this._blocking = blocking;
        }

        protected override CachingBackendFeatures CreateFeatures() => new Features( this._blocking );

        protected override void SetItemCore( string key, CacheItem item )
        {
            this._items[key] = item;

            // Track dependencies
            if ( !item.Dependencies.IsDefaultOrEmpty )
            {
                foreach ( var dep in item.Dependencies )
                {
                    this._dependencies.AddOrUpdate(
                        dep,
                        _ => new HashSet<string> { key },
                        ( _, set ) =>
                        {
                            lock ( set )
                            {
                                set.Add( key );
                            }

                            return set;
                        } );
                }
            }
        }

        protected override CacheItem? GetItemCore( string key, bool includeDependencies )
        {
            return this._items.TryGetValue( key, out var item ) ? item : null;
        }

        protected override bool ContainsItemCore( string key )
        {
            return this._items.ContainsKey( key );
        }

        protected override void RemoveItemCore( string key )
        {
            if ( this._items.TryRemove( key, out _ ) )
            {
                this.OnItemRemoved( key, CacheItemRemovedReason.Removed, this.Id );
            }
        }

        protected override void InvalidateDependencyCore( string key )
        {
            if ( this._dependencies.TryGetValue( key, out var dependentKeys ) )
            {
                List<string> keysToRemove;

                lock ( dependentKeys )
                {
                    keysToRemove = dependentKeys.ToList();
                }

                foreach ( var dependentKey in keysToRemove )
                {
                    this._items.TryRemove( dependentKey, out _ );
                }
            }

            this.OnDependencyInvalidated( key, this.Id );
        }

        protected override bool ContainsDependencyCore( string key )
        {
            return this._dependencies.ContainsKey( key );
        }

        protected override void ClearCore( ClearCacheOptions options )
        {
            this._items.Clear();
            this._dependencies.Clear();
        }

        private sealed class Features : CachingBackendFeatures
        {
            public Features( bool blocking )
            {
                this.Blocking = blocking;
            }

            public override bool Blocking { get; }

            public override bool Dependencies => true;

            public override bool ContainsDependency => true;
        }
    }
}