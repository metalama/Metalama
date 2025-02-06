// Copyright (c) SharpCrafters s.r.o. See the LICENSE.md file in the root directory of this repository root for details.

using System.Collections.Immutable;
using System.Linq;

namespace Metalama.LinqPad
{
    /// <summary>
    /// A facade object (view-model) representing an <see cref="IGrouping{TKey,TElement}"/>. 
    /// </summary>
    internal sealed class GroupingFacade<TKey, TItems>
    {
        public GroupingFacade( IGrouping<TKey, TItems> underlying )
        {
            this.Key = underlying.Key;
            this.Items = [..underlying];
        }

        public TKey Key { get; }

        public ImmutableArray<TItems> Items { get; }

        public override string ToString() => $"{this.Key} => {this.Items.Length} item(s)";
    }
}