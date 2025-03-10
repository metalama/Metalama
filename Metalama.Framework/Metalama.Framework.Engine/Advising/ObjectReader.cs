// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either an MIT license
// or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Services;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Advising
{
    /// <summary>
    /// Wraps an anonymous type into a dictionary-like <see cref="IObjectReader"/>.
    /// </summary>
    internal sealed class ObjectReader : IObjectReader
    {
        private readonly ObjectReaderFactory _objectReaderFactory;
        private readonly ProjectServiceProvider _serviceProvider;
        private ObjectReaderTypeAdapter? _typeAdapter;

        private ObjectReaderTypeAdapter TypeAdapter => this._typeAdapter ??= this._objectReaderFactory.GetTypeAdapter( this.Source.GetType() );

        public static readonly IObjectReader Empty = new ObjectReaderDictionaryWrapper( ImmutableDictionary<string, object?>.Empty );

        internal ObjectReader( object instance, ObjectReaderFactory objectReaderFactory, in ProjectServiceProvider serviceProvider )
        {
            this._objectReaderFactory = objectReaderFactory;
            this._serviceProvider = serviceProvider;
            this.Source = instance;
        }

        public static IObjectReader Merge( params IObjectReader?[] readers )
        {
            var nonEmptyCount = 0;
            var nonEmptyIndex = -1;

            for ( var i = 0; i < readers.Length; i++ )
            {
                if ( readers[i] != null && readers[i]!.Count > 0 )
                {
                    nonEmptyIndex = i;
                    nonEmptyCount++;

                    if ( nonEmptyCount >= 2 )
                    {
                        break;
                    }
                }
            }

            return nonEmptyCount switch
            {
                0 => Empty,
                1 => readers[nonEmptyIndex].AssertNotNull(),
                _ => new ObjectReaderMergeWrapper( readers )
            };
        }

        public object? this[ string key ]
        {
            get
            {
                if ( !this.TryGetValue( key, out var value ) )
                {
                    throw new KeyNotFoundException( $"The tag '{key}' is not defined." );
                }

                return value;
            }
        }

        public IEnumerable<string> Keys => this.TypeAdapter.Properties;

        public IEnumerable<object?> Values => this.TypeAdapter.Properties.Select( p => this[p] );

        public bool ContainsKey( string key ) => this.TypeAdapter.ContainsProperty( key );

        public bool TryGetValue( string key, out object? value ) => this.TypeAdapter.TryGetValue( this._serviceProvider, key, this.Source, out value );

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            => this.TypeAdapter.Properties.Select( p => new KeyValuePair<string, object?>( p, this[p] ) ).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        public int Count => this.TypeAdapter.PropertyCount;

        public object Source { get; }
    }
}