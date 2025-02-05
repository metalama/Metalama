// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Collections
{
    public sealed partial class ImmutableDictionaryOfArray<TKey, TValue>
    {
        internal class Group : IGrouping<TKey, TValue>
        {
            public ImmutableArray<TValue> Items { get; }

            public Group( TKey key, ImmutableArray<TValue> items )
            {
#if DEBUG
                if ( items.IsDefault )
                {
                    throw new ArgumentNullException( nameof(items) );
                }
#endif

                this.Key = key;
                this.Items = items;
            }

            public TKey Key { get; }

            public IEnumerator<TValue> GetEnumerator()
            {
                foreach ( var value in this.Items )
                {
                    yield return value;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            public Group Add( TValue value ) => new( this.Key, this.Items.Add( value ) );

            public override string ToString() => $"Key={this.Key}, Items={this.Items.Length}";
        }
    }
}