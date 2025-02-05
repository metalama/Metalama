// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Metalama.Framework.Engine.Collections
{
    public sealed partial class ImmutableDictionaryOfArray<TKey, TValue>
    {
        [PublicAPI]
        public sealed class Builder
        {
            private readonly ImmutableDictionaryOfArray<TKey, TValue>? _initialValues;

            private readonly Dictionary<TKey, ImmutableArray<TValue>.Builder> _modifiedValuesBuilder;

            // Creates a Builder initialized to an empty dictionary.
            internal Builder( IEqualityComparer<TKey>? comparer = null )
            {
                this._modifiedValuesBuilder = new Dictionary<TKey, ImmutableArray<TValue>.Builder>( comparer );
            }

            internal Builder( ImmutableDictionaryOfArray<TKey, TValue> initialValues )
            {
                this._initialValues = initialValues;
                this._modifiedValuesBuilder = new Dictionary<TKey, ImmutableArray<TValue>.Builder>( initialValues._dictionary.KeyComparer );
            }

            public void Add( TKey key, TValue value )
            {
                if ( !this._modifiedValuesBuilder.TryGetValue( key, out var arrayBuilder ) )
                {
                    if ( this._initialValues?._dictionary.TryGetValue( key, out var existingGroup ) == true )
                    {
                        arrayBuilder = existingGroup.Items.ToBuilder();
                    }
                    else
                    {
                        arrayBuilder = ImmutableArray.CreateBuilder<TValue>();
                    }

                    this._modifiedValuesBuilder[key] = arrayBuilder;
                }

                arrayBuilder.Add( value );
            }

            public void Add( TKey key, IEnumerable<TValue> values )
            {
                if ( !this._modifiedValuesBuilder.TryGetValue( key, out var arrayBuilder ) )
                {
                    if ( this._initialValues?._dictionary.TryGetValue( key, out var existingGroup ) == true )
                    {
                        arrayBuilder = existingGroup.Items.ToBuilder();
                    }
                    else
                    {
                        arrayBuilder = ImmutableArray.CreateBuilder<TValue>();
                    }

                    this._modifiedValuesBuilder[key] = arrayBuilder;
                }

                arrayBuilder.AddRange( values );
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

            public void AddRange( TKey key, IEnumerable<TValue> values )
            {
                if ( !this._modifiedValuesBuilder.TryGetValue( key, out var arrayBuilder ) )
                {
                    arrayBuilder = ImmutableArray.CreateBuilder<TValue>();
                    this._modifiedValuesBuilder[key] = arrayBuilder;
                }

                arrayBuilder.AddRange( values );
            }

            public ImmutableDictionaryOfArray<TKey, TValue> ToImmutable()
            {
                if ( this._modifiedValuesBuilder.Count == 0 )
                {
                    return this._initialValues ?? Create( this._modifiedValuesBuilder.Comparer );
                }
                else
                {
                    var dictionaryBuilder = this._initialValues?._dictionary.ToBuilder()
                                            ?? ImmutableDictionary.CreateBuilder<TKey, Group>( this._modifiedValuesBuilder.Comparer );

                    foreach ( var modifiedGroup in this._modifiedValuesBuilder )
                    {
                        var group = new Group( modifiedGroup.Key, modifiedGroup.Value.ToImmutable() );
                        dictionaryBuilder[modifiedGroup.Key] = group;
                    }

                    return new ImmutableDictionaryOfArray<TKey, TValue>( dictionaryBuilder.ToImmutable() );
                }
            }

            public bool Remove( TKey key, TValue value )
            {
                if ( !this._modifiedValuesBuilder.TryGetValue( key, out var arrayBuilder ) )
                {
                    if ( this._initialValues?._dictionary.TryGetValue( key, out var existingGroup ) == true )
                    {
                        arrayBuilder = existingGroup.Items.ToBuilder();
                        this._modifiedValuesBuilder[key] = arrayBuilder;
                    }
                    else
                    {
                        return false;
                    }
                }

                return arrayBuilder.Remove( value );
            }

            public void Remove( TKey key, IEnumerable<TValue> values )
            {
                if ( !this._modifiedValuesBuilder.TryGetValue( key, out var arrayBuilder ) )
                {
                    if ( this._initialValues?._dictionary.TryGetValue( key, out var existingGroup ) == true )
                    {
                        arrayBuilder = existingGroup.Items.ToBuilder();
                        this._modifiedValuesBuilder[key] = arrayBuilder;
                    }
                    else
                    {
                        return;
                    }
                }

                foreach ( var value in values )
                {
                    arrayBuilder.Remove( value );
                }
            }
        }
    }
}