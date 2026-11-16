// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;

namespace Metalama.Introspection.Navigation.Results;

/// <summary>
/// A collection, either collapsed to its count (<see cref="Items"/> is <c>null</c>) or expanded to its first items.
/// </summary>
internal sealed class CollectionNode : ResultNode
{
    public CollectionNode( string typeName, int? count )
    {
        this.TypeName = typeName;
        this.Count = count;
    }

    public string TypeName { get; }

    /// <summary>
    /// Gets the number of items, or <c>null</c> when it is unknown because the collection has no <c>Count</c> or <c>Length</c>
    /// and was not enumerated to the end.
    /// </summary>
    public int? Count { get; }

    /// <summary>
    /// Gets a value indicating whether <see cref="Count"/> was obtained by enumerating the collection rather than from its
    /// <c>Count</c> or <c>Length</c>. A collapsed collection shows only a count that needs no enumeration.
    /// </summary>
    public bool IsCountFromEnumeration { get; init; }

    public ImmutableArray<ResultNode>? Items { get; init; }

    /// <summary>
    /// Gets a value indicating whether the collection has more items than <see cref="Items"/>.
    /// </summary>
    public bool IsTruncated { get; init; }
}
