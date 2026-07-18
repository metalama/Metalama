// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Collections
{
    /// <summary>
    /// A dictionary that maps each key to a list of values, i.e. a <see cref="Dictionary{TKey,TValue}"/> of
    /// <see cref="List{T}"/>. Use this instead of <see cref="ImmutableDictionaryOfArray{TKey,TValue}"/> when the map
    /// is built once and thereafter only read, i.e. when the persistent-update capability of the immutable variant
    /// (its builder, and its cheap <c>Add</c>/<c>Merge</c> that share structure between versions) is not needed.
    /// </summary>
    /// <remarks>
    /// Mutating members are exposed so the map can be populated, but the intended use is to build it and then hand it
    /// out as <see cref="IReadOnlyDictionaryOfList{TKey,TValue}"/>. It is not thread-safe, and it does not attempt to
    /// prevent mutation after publication; when either matters, use
    /// <see cref="ImmutableDictionaryOfArray{TKey,TValue}"/>.
    /// </remarks>
    [PublicAPI]
    public sealed class DictionaryOfList<TKey, TValue> : IReadOnlyDictionaryOfList<TKey, TValue>
        where TKey : notnull
    {
        private static readonly IReadOnlyList<TValue> _emptyValues = Array.Empty<TValue>();

        private readonly Dictionary<TKey, List<TValue>> _dictionary;
        private bool _isFrozen;

        public DictionaryOfList( IEqualityComparer<TKey>? keyComparer = null )
        {
            this._dictionary = new Dictionary<TKey, List<TValue>>( keyComparer );
        }

        /// <summary>
        /// Gets a value indicating whether <see cref="Freeze"/> has been called, after which the map can no longer be
        /// modified.
        /// </summary>
        public bool IsFrozen => this._isFrozen;

        /// <summary>
        /// Prevents any further modification of this map and returns it as an
        /// <see cref="IReadOnlyDictionaryOfList{TKey,TValue}"/>, so that a map can be built and published in a single
        /// expression. Calling it more than once is harmless.
        /// </summary>
        /// <remarks>
        /// This guards this class's own mutating members. The values are handed out as
        /// <see cref="IReadOnlyList{T}"/> over the internal <see cref="List{T}"/> instances, so freezing does not
        /// defeat a caller that deliberately downcasts one of them back to <see cref="List{T}"/>.
        /// </remarks>
        public IReadOnlyDictionaryOfList<TKey, TValue> Freeze()
        {
            this._isFrozen = true;

            return this;
        }

        private void VerifyNotFrozen()
        {
            if ( this._isFrozen )
            {
                throw new InvalidOperationException(
                    $"This {nameof(DictionaryOfList<TKey, TValue>)} has been frozen and can no longer be modified." );
            }
        }

        public static DictionaryOfList<TKey, TValue> Create<TItem>(
            IEnumerable<TItem> source,
            Func<TItem, TKey> getKey,
            Func<TItem, TValue> getValue,
            IEqualityComparer<TKey>? keyComparer = null )
        {
            var result = new DictionaryOfList<TKey, TValue>( keyComparer );

            foreach ( var item in source )
            {
                result.Add( getKey( item ), getValue( item ) );
            }

            return result;
        }

        public static DictionaryOfList<TKey, TValue> Create(
            IEnumerable<TValue> source,
            Func<TValue, TKey> getKey,
            IEqualityComparer<TKey>? keyComparer = null )
            => Create( source, getKey, v => v, keyComparer );

        public IEqualityComparer<TKey> KeyComparer => this._dictionary.Comparer;

        public void Add( TKey key, TValue value )
        {
            this.VerifyNotFrozen();

            if ( !this._dictionary.TryGetValue( key, out var list ) )
            {
                list = new List<TValue>();
                this._dictionary.Add( key, list );
            }

            list.Add( value );
        }

        public void AddRange( TKey key, IEnumerable<TValue> values )
        {
            this.VerifyNotFrozen();

            if ( !this._dictionary.TryGetValue( key, out var list ) )
            {
                list = new List<TValue>();
                this._dictionary.Add( key, list );
            }

            list.AddRange( values );
        }

        /// <summary>
        /// Adds every item of <paramref name="source"/>, deriving the key and the value of each from the item.
        /// </summary>
        public void AddRange<TItem>( IEnumerable<TItem> source, Func<TItem, TKey> getKey, Func<TItem, TValue> getValue )
        {
            // Checked here too, so that freezing is reported even when the source turns out to be empty.
            this.VerifyNotFrozen();

            foreach ( var item in source )
            {
                this.Add( getKey( item ), getValue( item ) );
            }
        }

        /// <summary>
        /// Gets the values for the given key, or an empty list if the key is absent. See
        /// <see cref="IReadOnlyDictionaryOfList{TKey,TValue}.this"/> for why this does not throw.
        /// </summary>
        public IReadOnlyList<TValue> this[ TKey key ]
            => this._dictionary.TryGetValue( key, out var list ) ? list : _emptyValues;

        public IEnumerable<TKey> Keys => this._dictionary.Keys;

        public IEnumerable<IReadOnlyList<TValue>> Values => this._dictionary.Values;

        public int Count => this._dictionary.Count;

        public bool IsEmpty => this._dictionary.Count == 0;

        public bool ContainsKey( TKey key ) => this._dictionary.ContainsKey( key );

        public bool TryGetValue( TKey key, out IReadOnlyList<TValue> value )
        {
            if ( this._dictionary.TryGetValue( key, out var list ) )
            {
                value = list;

                return true;
            }

            value = _emptyValues;

            return false;
        }

        public IEnumerator<KeyValuePair<TKey, IReadOnlyList<TValue>>> GetEnumerator()
        {
            foreach ( var pair in this._dictionary )
            {
                yield return new KeyValuePair<TKey, IReadOnlyList<TValue>>( pair.Key, pair.Value );
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
