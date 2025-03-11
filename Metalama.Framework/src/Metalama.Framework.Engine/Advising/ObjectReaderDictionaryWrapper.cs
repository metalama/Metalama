// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using Metalama.Framework.Aspects;
using System.Collections;
using System.Collections.Generic;

namespace Metalama.Framework.Engine.Advising
{
    internal sealed class ObjectReaderDictionaryWrapper : IObjectReader
    {
        private readonly IReadOnlyDictionary<string, object?> _inner;

        public ObjectReaderDictionaryWrapper( IReadOnlyDictionary<string, object?> inner )
        {
            this._inner = inner;
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

        public object Source => this._inner;

        public IEnumerable<string> Keys => this._inner.Keys;

        public IEnumerable<object?> Values => this._inner.Values;

        public int Count => this._inner.Count;

        public bool ContainsKey( string key ) => this._inner.ContainsKey( key );

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => this._inner.GetEnumerator();

        public bool TryGetValue( string key, out object? value ) => this._inner.TryGetValue( key, out value );

        IEnumerator IEnumerable.GetEnumerator() => this._inner.GetEnumerator();
    }
}