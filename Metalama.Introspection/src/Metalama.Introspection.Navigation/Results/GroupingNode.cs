// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

namespace Metalama.Introspection.Navigation.Results;

/// <summary>
/// A group of a <c>GroupBy</c>.
/// </summary>
internal sealed class GroupingNode : ResultNode
{
    public GroupingNode( ResultNode key, CollectionNode items )
    {
        this.Key = key;
        this.Items = items;
    }

    public ResultNode Key { get; }

    public CollectionNode Items { get; }
}
