// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Collections
{
    public sealed partial class ImmutableDictionaryOfHashSet<TKey, TValue>
    {
        [PublicAPI]
        public sealed class Builder
        {
            private readonly IEqualityComparer<TValue> _valueComparer;
            private readonly ImmutableDictionaryOfHashSet<TKey, TValue>? _initialValues;

            private readonly ImmutableDictionary<TKey, ImmutableHashSet<TValue>.Builder>.Builder _newValuesBuilder;

            // Creates a Builder initialized to an empty dictionary.
            internal Builder( IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer )
            {
                this._valueComparer = valueComparer;
                this._newValuesBuilder = ImmutableDictionary.CreateBuilder<TKey, ImmutableHashSet<TValue>.Builder>( keyComparer );
            }

            internal Builder( ImmutableDictionaryOfHashSet<TKey, TValue> initialValues )
            {
                this._initialValues = initialValues;
                this._newValuesBuilder = ImmutableDictionary.CreateBuilder<TKey, ImmutableHashSet<TValue>.Builder>( initialValues._dictionary.KeyComparer );
                this._valueComparer = initialValues.ValueComparer;
            }

            public void Add( TKey key, TValue value )
            {
                var hashSetBuilder = this.GetHashSetBuilder( key )!;

                hashSetBuilder.Add( value );
            }

            private ImmutableHashSet<TValue>.Builder? GetHashSetBuilder( TKey key, bool create = true )
            {
                if ( !this._newValuesBuilder.TryGetValue( key, out var setBuilder ) )
                {
                    if ( this._initialValues != null && this._initialValues._dictionary.TryGetValue( key, out var existingGroup ) )
                    {
                        setBuilder = existingGroup.Items.ToBuilder();
                    }
                    else
                    {
                        if ( !create )
                        {
                            return null;
                        }

                        setBuilder = ImmutableHashSet.CreateBuilder( this._valueComparer );
                    }

                    this._newValuesBuilder[key] = setBuilder;
                }

                return setBuilder;
            }

            public void AddRange<TItem>( IEnumerable<TItem> source, Func<TItem, TKey> getKey, Func<TItem, TValue> getValue )
            {
                foreach ( var item in source )
                {
                    var key = getKey( item );
                    var value = getValue( item );

                    this.Add( key, value );
                }
            }

            public void AddRange<TItem>( IEnumerable<TItem> source, Func<TItem, TKey> getKey, Func<TItem, IEnumerable<TValue>> getValues )
            {
                foreach ( var item in source )
                {
                    var key = getKey( item );
                    var values = getValues( item );

                    this.AddRange( key, values );
                }
            }

            private void AddRange( TKey key, IEnumerable<TValue> values )
            {
                var hashSetBuilder = this.GetHashSetBuilder( key )!;

                foreach ( var value in values )
                {
                    hashSetBuilder.Add( value );
                }
            }

            public ImmutableDictionaryOfHashSet<TKey, TValue> ToImmutable()
            {
                if ( this._newValuesBuilder.Count == 0 )
                {
                    return this._initialValues ?? Empty;
                }
                else
                {
                    var dictionaryBuilder = this._initialValues?._dictionary.ToBuilder()
                                            ?? ImmutableDictionary.CreateBuilder<TKey, Group>( this._newValuesBuilder.KeyComparer );

                    foreach ( var newGroup in this._newValuesBuilder )
                    {
                        if ( newGroup.Value.Count > 0 )
                        {
                            var group = new Group( newGroup.Key, newGroup.Value.ToImmutable() );

                            dictionaryBuilder[group.Key] = group;
                        }
                        else
                        {
                            dictionaryBuilder.Remove( newGroup.Key );
                        }
                    }

                    return new ImmutableDictionaryOfHashSet<TKey, TValue>( dictionaryBuilder.ToImmutable(), this._valueComparer );
                }
            }

            public bool Remove( TKey key, TValue value )
            {
                var hashSetBuilder = this.GetHashSetBuilder( key, false );

                if ( hashSetBuilder != null )
                {
                    if ( hashSetBuilder.Remove( value ) )
                    {
                        // We intentionally do not remove the hashSetBuilder from the collection
                        // because empty hashsets will be removed by ToImmutable, while missing hashsets mean that they are unchanged.

                        return true;
                    }
                }

                return false;
            }
        }
    }
}