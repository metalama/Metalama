// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using Metalama.Framework.Aspects;
using Metalama.Framework.Engine.Utilities;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Advising
{
    internal sealed class ObjectReaderMergeWrapper : IObjectReader
    {
        private readonly IObjectReader?[] _readers;
        private ImmutableDictionary<string, object?>? _dictionary;

        private ImmutableDictionary<string, object?> Dictionary => this._dictionary ??= this.BuildDictionary();
        
        public ObjectReaderMergeWrapper( params IObjectReader?[] readers )
        {
            this._readers = readers;
        }

        private ImmutableDictionary<string, object?> BuildDictionary()
        {
            var dictionaryBuilder = ImmutableDictionary<string, object?>.Empty.ToBuilder();
        
            foreach ( var reader in this._readers )
            {
                if ( reader == null )
                {
                    continue;
                }

                foreach ( var kvp in reader )
                {
                    dictionaryBuilder[kvp.Key] = kvp.Value;
                }
            }

            return dictionaryBuilder.ToImmutable();
        }

        public object? this[ string key ] => this.Dictionary[key];

        [Memo]
        public object Source
            => this._readers.Where( x => x != null )
                .Select( x => x!.Source )
                .Where( x => x != null )
                .ToImmutableArray();

        public IEnumerable<string> Keys => this.Dictionary.Keys;

        public IEnumerable<object?> Values => this.Dictionary.Values;

        public int Count => this.Dictionary.Count;

        public bool ContainsKey( string key ) => this.Dictionary.ContainsKey( key );

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator() => this.Dictionary.GetEnumerator();

        public bool TryGetValue( string key, out object? value ) => this.Dictionary.TryGetValue( key, out value );

        IEnumerator IEnumerable.GetEnumerator() => this.Dictionary.GetEnumerator();
    }
}