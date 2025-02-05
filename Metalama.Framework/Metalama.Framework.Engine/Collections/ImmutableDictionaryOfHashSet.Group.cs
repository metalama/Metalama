// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Framework.Engine.Collections
{
    public sealed partial class ImmutableDictionaryOfHashSet<TKey, TValue>
    {
        private class Group : IGrouping<TKey, TValue>
        {
            public ImmutableHashSet<TValue> Items { get; }

            public Group( TKey key, ImmutableHashSet<TValue> items )
            {
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

            public override string ToString() => $"Key={this.Key}, Items={this.Items.Count}";
        }
    }
}