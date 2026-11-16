// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

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