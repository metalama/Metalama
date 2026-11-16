// Copyright (c) 2020-2025 SharpCrafters s.r.o. and contributors.
// SharpCrafters s.r.o. licenses this file to you under either the MIT license or a proprietary license, depending on the repository from which it was obtained.
// Refer to LICENSE.md in the repository root for complete details.

using System.Collections.Immutable;
using System.Linq;

namespace Metalama.Introspection.Navigation.Results;

/// <summary>
/// Cuts a result tree to smaller limits, with the same expansion rules as <see cref="ResultTreeBuilder"/>. This lets one build
/// of a value serve several outputs, such as a deep JSON for a viewer and a shallow text for the model, without running the
/// query or enumerating its result twice.
/// </summary>
internal static class ResultTreePruner
{
    /// <summary>
    /// Returns the tree that <see cref="ResultTreeBuilder"/> would have built with <paramref name="options"/>, given a tree
    /// built with limits at least as large.
    /// </summary>
    public static ResultNode Prune( ResultNode root, ResultTreeOptions options ) => Prune( root, options, 0, true );

    private static ResultNode Prune( ResultNode node, ResultTreeOptions options, int depth, bool expand )
    {
        switch ( node )
        {
            case ObjectNode { Properties: { } properties } obj:
                if ( !expand )
                {
                    return CopyObject( obj, null );
                }

                return CopyObject(
                    obj,
                    properties.Select( p => p with { Value = Prune( p.Value, options, depth + 1, depth + 1 < options.MaxDepth ) } ).ToImmutableArray() );

            case CollectionNode collection:
                return PruneCollection( collection, options, depth, expand );

            case GroupingNode grouping:
                return new GroupingNode( Prune( grouping.Key, options, depth + 1, false ), PruneCollection( grouping.Items, options, depth, expand ) )
                {
                    Link = grouping.Link
                };

            case RichTextNode text:
                return new RichTextNode(
                    text.Text,
                    text.Segments.Select( s => s.Argument == null ? s : s with { Argument = Prune( s.Argument, options, depth + 1, false ) } )
                        .ToImmutableArray() ) { Link = text.Link };

            default:
                return node;
        }
    }

    private static CollectionNode PruneCollection( CollectionNode collection, ResultTreeOptions options, int depth, bool expand )
    {
        if ( !expand || collection.Items is not { } items )
        {
            // A collapsed collection shows only a count that needs no enumeration, as ResultTreeBuilder does.
            var count = collection.IsCountFromEnumeration ? null : collection.Count;

            return new CollectionNode( collection.TypeName, count ) { Link = collection.Link };
        }

        var kept = items
            .Take( options.MaxItems )
            .Select( item => Prune( item, options, depth + 1, item is ObjectNode { IsDeclaration: true } ? depth + 1 < options.MaxDepth : depth < options.MaxDepth ) )
            .ToImmutableArray();

        var isTruncated = collection.IsTruncated || items.Length > kept.Length;

        // When items are dropped, a count that came from enumerating them all is no longer known to the smaller tree.
        var keptCount = collection.IsCountFromEnumeration && isTruncated ? null : collection.Count;

        return new CollectionNode( collection.TypeName, keptCount )
        {
            Link = collection.Link, Items = kept, IsTruncated = isTruncated, IsCountFromEnumeration = collection.IsCountFromEnumeration && !isTruncated
        };
    }

    private static ObjectNode CopyObject( ObjectNode obj, ImmutableArray<PropertyNode>? properties )
        => new( obj.TypeName, obj.Display )
        {
            Link = obj.Link,
            Location = obj.Location,
            InterfaceName = obj.InterfaceName,
            IsDeclaration = obj.IsDeclaration,
            IsTransformed = obj.IsTransformed,
            Properties = properties
        };
}
