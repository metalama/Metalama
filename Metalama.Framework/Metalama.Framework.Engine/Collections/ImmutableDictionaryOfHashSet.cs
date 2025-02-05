// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Collections
{
    [PublicAPI]
    public sealed partial class ImmutableDictionaryOfHashSet<TKey, TValue> : IReadOnlyMultiValueDictionary<TKey, TValue>
        where TKey : notnull
    {
        private readonly ImmutableDictionary<TKey, Group> _dictionary;

        public IEqualityComparer<TKey> KeyComparer => this._dictionary.KeyComparer;

        public IEqualityComparer<TValue> ValueComparer { get; }

        private ImmutableDictionaryOfHashSet( ImmutableDictionary<TKey, Group> dictionary, IEqualityComparer<TValue> valueComparer )
        {
            this._dictionary = dictionary;
            this.ValueComparer = valueComparer;
        }

        internal ImmutableDictionaryOfHashSet( IEnumerable<KeyValuePair<TKey, ImmutableHashSet<TValue>>> items, IEqualityComparer<TKey> keyComparer )
        {
            this._dictionary = items.ToImmutableDictionary(
                i => i.Key,
                i => new Group( i.Key, i.Value ),
                keyComparer );

            this.ValueComparer = EqualityComparer<TValue>.Default;
        }

        public static ImmutableDictionaryOfHashSet<TKey, TValue> Empty => new( ImmutableDictionary<TKey, Group>.Empty, EqualityComparer<TValue>.Default );

        public static ImmutableDictionaryOfHashSet<TKey, TValue> Create( IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer )
            => new( ImmutableDictionary.Create<TKey, Group>( keyComparer ), valueComparer );

        // Coverage: ignore
        public static ImmutableDictionaryOfHashSet<TKey, TValue> Create(
            IEnumerable<TValue> source,
            Func<TValue, TKey> getKey,
            IEqualityComparer<TKey>? comparer = null )
            => Create( source, getKey, v => v, comparer );

        public static ImmutableDictionaryOfHashSet<TKey, TValue> Create<TItem>(
            IEnumerable<TItem> source,
            Func<TItem, TKey> getKey,
            Func<TItem, TValue> getValue,
            IEqualityComparer<TKey>? keyComparer = null,
            IEqualityComparer<TValue>? valueComparer = null )
        {
            var builder = new Builder( keyComparer ?? EqualityComparer<TKey>.Default, valueComparer ?? EqualityComparer<TValue>.Default );
            builder.AddRange( source, getKey, getValue );

            return builder.ToImmutable();
        }

        public static Builder CreateBuilder( IEqualityComparer<TKey>? keyComparer = null, IEqualityComparer<TValue>? valueComparer = null )
            => new( keyComparer ?? EqualityComparer<TKey>.Default, valueComparer ?? EqualityComparer<TValue>.Default );

        public ImmutableDictionaryOfHashSet<TKey, TValue> AddRange( IEnumerable<TValue> source, Func<TValue, TKey> getKey )
            => this.AddRange( source, getKey, v => v );

        public ImmutableDictionaryOfHashSet<TKey, TValue> AddRange<TItem>( IEnumerable<TItem> source, Func<TItem, TKey> getKey, Func<TItem, TValue> getValue )
        {
            var builder = this.ToBuilder();
            builder.AddRange( source, getKey, getValue );

            return builder.ToImmutable();
        }

        public bool IsEmpty => this._dictionary.IsEmpty;

        IReadOnlyCollection<TValue> IReadOnlyMultiValueDictionary<TKey, TValue>.this[ TKey key ] => this[key];

        public ImmutableHashSet<TValue> this[ TKey key ]
        {
            get
            {
                if ( this._dictionary.TryGetValue( key, out var group ) )
                {
                    return group.Items;
                }

                return ImmutableHashSet<TValue>.Empty;
            }
        }

        public IEnumerable<TKey> Keys => this._dictionary.Keys;

        public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator() => this._dictionary.Values.Cast<IGrouping<TKey, TValue>>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public Builder ToBuilder() => new( this );

        public ImmutableDictionaryOfHashSet<TKey, TValue> Merge( IEnumerable<ImmutableDictionaryOfHashSet<TKey, TValue>> others )
        {
            // We optimize for low conflicts in keys i.e. each dictionary has mostly disjoint set of keys.

            ImmutableDictionary<TKey, Group>.Builder? builder = null;

            foreach ( var other in others )
            {
                if ( other.IsEmpty )
                {
                    continue;
                }

                builder ??= this._dictionary.ToBuilder();

                foreach ( var pair in other._dictionary )
                {
                    if ( builder.TryGetValue( pair.Key, out var currentGroup ) )
                    {
                        builder[pair.Key] = new Group( pair.Key, currentGroup.Items.AddRange( pair.Value.Items ) );
                    }
                    else
                    {
                        builder[pair.Key] = pair.Value;
                    }
                }
            }

            if ( builder == null )
            {
                return this;
            }
            else
            {
                return new ImmutableDictionaryOfHashSet<TKey, TValue>( builder.ToImmutable(), this.ValueComparer );
            }
        }
    }
}